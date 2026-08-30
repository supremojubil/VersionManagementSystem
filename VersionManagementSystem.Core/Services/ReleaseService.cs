using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Enums;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.Core.Services {
    public sealed class ReleaseService : IReleaseService {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationVersionRepository _versionRepository;
        private readonly IReleaseNoteRepository _releaseNoteRepository;
        private readonly IUpdatePackageRepository _packageRepository;

        public ReleaseService(IApplicationRepository applicationRepository, IApplicationVersionRepository versionRepository, IReleaseNoteRepository releaseNoteRepository, IUpdatePackageRepository packageRepository) {
            _applicationRepository = applicationRepository;
            _versionRepository = versionRepository;
            _releaseNoteRepository = releaseNoteRepository;
            _packageRepository = packageRepository;
        }

        public async Task<ApplicationVersionDTO> SubmitForTestingAsync(string applicationCode, string version) {
            var entity = await GetVersionOrThrowAsync(applicationCode, version);
            Transition(entity, from: ReleaseStatus.Draft, to: ReleaseStatus.Testing);

            await _versionRepository.UpdateAsync(entity);
            await _versionRepository.SaveChangesAsync();

            return await BuildDtoAsync(entity, applicationCode);
        }

        public async Task<ApplicationVersionDTO> ApproveAsync(string applicationCode, string version) {
            var entity = await GetVersionOrThrowAsync(applicationCode, version);
            Transition(entity, from: ReleaseStatus.Testing, to: ReleaseStatus.Approved);

            await _versionRepository.UpdateAsync(entity);
            await _versionRepository.SaveChangesAsync();

            return await BuildDtoAsync(entity, applicationCode);
        }

        public async Task<ApplicationVersionDTO> PublishAsync(string applicationCode, string version, string? publishedBy) {
            var entity = await GetVersionOrThrowAsync(applicationCode, version);
            Transition(entity, from: ReleaseStatus.Approved, to: ReleaseStatus.Published);

            // Prevent publishing invalid versions: a published version must have at least one package.
            if (!await _packageRepository.AnyForVersionAsync(entity.ApplicationVersionId)) {
                // Roll the in-memory status back before throwing — nothing has been saved yet.
                entity.ReleaseStatus = ReleaseStatus.Approved;
                throw new ValidationException($"Version {entity.VersionString} cannot be published without at least one uploaded update package.");
            }

            entity.PublishedDate = DateTime.UtcNow;
            entity.PublishedBy = publishedBy;

            await _versionRepository.UpdateAsync(entity);
            await _versionRepository.SaveChangesAsync();

            return await BuildDtoAsync(entity, applicationCode);
        }

        public async Task<ApplicationVersionDTO> DeprecateAsync(string applicationCode, string version) {
            var entity = await GetVersionOrThrowAsync(applicationCode, version);
            Transition(entity, from: ReleaseStatus.Published, to: ReleaseStatus.Deprecated);

            await _versionRepository.UpdateAsync(entity);
            await _versionRepository.SaveChangesAsync();

            return await BuildDtoAsync(entity, applicationCode);
        }

        public async Task<ApplicationVersionDTO> ArchiveAsync(string applicationCode, string version) {
            var entity = await GetVersionOrThrowAsync(applicationCode, version);
            Transition(entity, from: ReleaseStatus.Deprecated, to: ReleaseStatus.Archived);

            await _versionRepository.UpdateAsync(entity);
            await _versionRepository.SaveChangesAsync();

            return await BuildDtoAsync(entity, applicationCode);
        }

        public async Task<IReadOnlyList<ReleaseNoteDTO>> AddReleaseNotesAsync(string applicationCode, string version, IReadOnlyList<CreateReleaseNoteDTO> releaseNotes) {
            if (releaseNotes.Count == 0) {
                throw new ValidationException("At least one release note is required.");
            }

            var entity = await GetVersionOrThrowAsync(applicationCode, version);

            var notesToAdd = new List<ReleaseNote>(releaseNotes.Count);
            foreach (var note in releaseNotes) {
                if (!Enum.TryParse<ReleaseNoteCategory>(note.Category, ignoreCase: true, out var category)) {
                    throw new ValidationException($"'{note.Category}' is not a valid release note category. Expected NewFeature, Improvement or BugFix.");
                }

                if (string.IsNullOrWhiteSpace(note.Description)) {
                    throw new ValidationException("Release note description is required.");
                }

                notesToAdd.Add(new ReleaseNote {
                    ApplicationVersionId = entity.ApplicationVersionId,
                    Category = category,
                    Description = note.Description.Trim(),
                    SortOrder = note.SortOrder,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _releaseNoteRepository.AddRangeAsync(notesToAdd);
            await _releaseNoteRepository.SaveChangesAsync();

            return notesToAdd
                .OrderBy(c => c.Category)
                .ThenBy(c => c.SortOrder)
                .Select(MapNoteToDto)
                .ToList();
        }

        private static void Transition(ApplicationVersion entity, ReleaseStatus from, ReleaseStatus to) {
            if (entity.ReleaseStatus != from) {
                throw new ValidationException($"Version {entity.VersionString} is currently '{entity.ReleaseStatus}'. " + $"It must be '{from}' before it can move to '{to}'.");
            }

            entity.ReleaseStatus = to;
        }

        private async Task<ApplicationVersion> GetVersionOrThrowAsync(string applicationCode, string version) {
            var application = await _applicationRepository.GetByCodeAsync(applicationCode);
            if (application == null) {
                throw new NotFoundException($"Application '{applicationCode}' was not found.");
            }

            if (!Version.TryParse(version, out var parsedVersion) || parsedVersion is null || parsedVersion.Build < 0 || parsedVersion.Revision < 0) {
                throw new ValidationException($"'{version}' is not a valid .NET assembly version. Expected Major.Minor.Build.Revision (for example 2026.8.26.1).");
            }

            var entity = await _versionRepository.GetByVersionAsync(application.ApplicationId, parsedVersion.Major, parsedVersion.Minor, parsedVersion.Build, parsedVersion.Revision);

            if (entity == null) {
                throw new NotFoundException($"Version {parsedVersion.ToString(4)} was not found for application '{applicationCode}'.");
            }

            return entity;
        }

        private async Task<ApplicationVersionDTO> BuildDtoAsync(ApplicationVersion entity, string applicationCode) {
            var releaseNotes = await _releaseNoteRepository.GetByApplicationVersionIdAsync(entity.ApplicationVersionId);
            var packages = await _packageRepository.GetByApplicationVersionIdAsync(entity.ApplicationVersionId);

            return new ApplicationVersionDTO {
                ApplicationVersionId = entity.ApplicationVersionId,
                ApplicationId = entity.ApplicationId,
                ApplicationCode = applicationCode,
                Version = entity.VersionString,
                ReleaseType = entity.ReleaseType.ToString(),
                ReleaseStatus = entity.ReleaseStatus.ToString(),
                ReleaseDate = entity.ReleaseDate,
                ReleaseNotes = entity.ReleaseNotes,
                MinimumSupportedVersion = entity.MinimumSupportedVersion,
                IsMandatory = entity.IsMandatory,
                Channel = entity.Channel.ToString(),
                CreatedDate = entity.CreatedDate,
                CreatedBy = entity.CreatedBy,
                PublishedDate = entity.PublishedDate,
                PublishedBy = entity.PublishedBy,
                StructuredReleaseNotes = releaseNotes
                    .OrderBy(n => n.Category)
                    .ThenBy(n => n.SortOrder)
                    .Select(MapNoteToDto)
                    .ToList(),
                Packages = packages.Select(p => new UpdatePackageDTO {
                    UpdatePackageId = p.UpdatePackageId,
                    ApplicationVersionId = p.ApplicationVersionId,
                    ApplicationCode = applicationCode,
                    Version = entity.VersionString,
                    FileName = p.FileName,
                    FileSize = p.FileSize,
                    Checksum = p.Checksum,
                    PackageType = p.PackageType.ToString(),
                    CreatedDate = p.CreatedDate,
                    UploadedBy = p.UploadedBy
                }).ToList()
            };
        }

        private static ReleaseNoteDTO MapNoteToDto(ReleaseNote note) {
            return new ReleaseNoteDTO {
                ReleaseNoteId = note.ReleaseNoteId,
                Category = note.Category.ToString(),
                Description = note.Description,
                SortOrder = note.SortOrder
            };
        }
    }
}
