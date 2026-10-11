namespace CodeHub.Api.Endpoints;

using CodeHub.Application.Abstractions;
using CodeHub.Application.Common;
using CodeHub.Application.Common.Mapping;
using CodeHub.Application.Contracts;
using CodeHub.Application.Features.File;
using CodeHub.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/file").WithTags("File");

        group.MapPost("/{repositoryId:int}/files",
            async (int repositoryId,
                HttpRequest request,
                IDispatcher dispatcher,
                ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var files = request.Form.Files.GetFiles("files");
                var paths = request.Form["paths"].ToArray();

                if (files.Count != paths.Count())
                {
                    throw AppException.BadRequest("files count and paths count mismatch");
                }

                var user = await currentUser.GetUserAsync(cancellationToken);

                var uploads = await ReadUploadsAsync(files, paths, cancellationToken);

                await dispatcher.SendAsync(new UploadFilesCommand(user.Id, repositoryId, uploads), cancellationToken);

                return TypedResults.Ok(ApiResponse<object>.Ok());
            })
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapPost("/{repositoryId:int}/import",
            async (int repositoryId,
                HttpRequest request,
                IDispatcher dispatcher,
                ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var zipFile = request.Form.Files.GetFile("zip_file")
                    ?? throw AppException.BadRequest("zip_file is required");

                var user = await currentUser.GetUserAsync(cancellationToken);

                using var stream = zipFile.OpenReadStream();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken);

                await dispatcher.SendAsync(
                    new ImportZipCommand(user.Id, repositoryId, zipFile.FileName, buffer.ToArray()),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<object>.Ok());
            })
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapGet("/{repositoryId:int}/files",
            async (int repositoryId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var files = await dispatcher.QueryAsync<ListRepositoryFilesQuery, IReadOnlyList<RepositoryFile>>(
                    new ListRepositoryFilesQuery(repositoryId),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<IReadOnlyList<FileResponse>>.Ok(
                    files.Select(f => f.ToResponse()).ToList()));
            });

        group.MapGet("/{repositoryId:int}/files/{fileId:int}/content",
            async (int repositoryId, int fileId, HttpContext httpContext, IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var content = await dispatcher.QueryAsync<GetFileContentQuery, FileContentData>(
                    new GetFileContentQuery(repositoryId, fileId),
                    cancellationToken);

                var downloadUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}" +
                                  $"/api/v1/file/{repositoryId}/files/{fileId}/download";

                return TypedResults.Ok(ApiResponse<FileContentResponse>.Ok(
                    new FileContentResponse(
                        content.Id,
                        content.FileName,
                        content.RelativePath,
                        content.Content,
                        content.IsBinary,
                        downloadUrl,
                        content.FileSize)));
            });

        group.MapGet("/{repositoryId:int}/files/{fileId:int}/download",
            async (int repositoryId, int fileId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var file = await dispatcher.QueryAsync<DownloadFileQuery, RepositoryFile>(
                    new DownloadFileQuery(repositoryId, fileId),
                    cancellationToken);

                return Results.File(file.FilePath, "application/octet-stream", file.FileName);
            });

        group.MapDelete("/{repositoryId:int}/files/{fileId:int}",
            async (int repositoryId, int fileId, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                var deleted = await dispatcher.SendAsync<DeleteFileCommand, bool>(
                    new DeleteFileCommand(user.Id, repositoryId, fileId),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<bool>.Ok(deleted));
            })
            .RequireAuthorization();

        group.MapGet("/{repositoryId:int}/download",
            async (int repositoryId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var zipPath = await dispatcher.QueryAsync<DownloadRepositoryQuery, string>(
                    new DownloadRepositoryQuery(repositoryId),
                    cancellationToken);

                return Results.File(zipPath, "application/zip", Path.GetFileName(zipPath));
            });

        group.MapPost("/vulnerability",
            async (AnalyzeFileRequest request, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                var result = await dispatcher.SendAsync<AnalyzeVulnerabilityCommand, string>(
                    new AnalyzeVulnerabilityCommand(user.Id, request.FileId, request.EnResponse),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<VulnerabilityFileResponse>.Ok(
                    new VulnerabilityFileResponse(request.FileId, result)));
            })
            .RequireAuthorization();

        group.MapPost("/optimization",
            async (OptimizationFileRequest request, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                var result = await dispatcher.SendAsync<OptimizeFileCommand, string>(
                    new OptimizeFileCommand(user.Id, request.FileId),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<OptimizationFileResponse>.Ok(
                    new OptimizationFileResponse(request.FileId, result)));
            })
            .RequireAuthorization();

        group.MapPut("/changeContent",
            async (ChangeFileContentRequest request, IDispatcher dispatcher, ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var user = await currentUser.GetUserAsync(cancellationToken);

                await dispatcher.SendAsync(
                    new ChangeFileContentCommand(user.Id, request.FileId, request.Content),
                    cancellationToken);

                return TypedResults.Ok(ApiResponse<object>.Ok());
            })
            .RequireAuthorization();

        return app;
    }

    private static async Task<List<FileUpload>> ReadUploadsAsync(
        IReadOnlyList<IFormFile> files,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken)
    {
        var uploads = new List<FileUpload>(files.Count);

        for (var i = 0; i < files.Count; i++)
        {
            using var stream = files[i].OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            uploads.Add(new FileUpload(files[i].FileName, paths[i], buffer.ToArray()));
        }

        return uploads;
    }
}
