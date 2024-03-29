﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AniSort.Core.Data.Repositories;

public interface ILocalFileRepository : IRepository<LocalFile, Guid>
{
    /// <summary>
    /// Gets first file with a matching ED2k hash
    /// </summary>
    /// <param name="hash">Hash to match</param>
    /// <returns>First local file with the matching hash if it exists, otherwise null</returns>
    LocalFile? GetFirstForEd2kHash(byte[] hash);
    
    /// <summary>
    /// Gets first file with a matching ED2k hash
    /// </summary>
    /// <param name="hash">Hash to match</param>
    /// <returns>First local file with the matching hash if it exists, otherwise null</returns>
    Task<LocalFile?> GetFirstForEd2kHashAsync(byte[] hash);

    /// <summary>
    /// Gets files with a matching ED2k hash
    /// </summary>
    /// <param name="hash">Hash to match</param>
    /// <returns>Files with matching hash if it exists, otherwise default</returns>
    IEnumerable<LocalFile> GetForEd2kHash(byte[] hash);
    
    /// <summary>
    /// Gets files with a matching ED2k hash
    /// </summary>
    /// <param name="hash">Hash to match</param>
    /// <returns>Files with matching hash if it exists, otherwise default</returns>
    IAsyncEnumerable<LocalFile> GetForEd2kHashAsync(byte[] hash);

    /// <summary>
    /// Check if any files exist for the ED2k hash
    /// </summary>
    /// <param name="hash">Hash to match</param>
    /// <returns>Existence</returns>
    bool ExistsForEd2kHash(byte[] hash);
    
    /// <summary>
    /// Check if any files exist for the ED2k hash
    /// </summary>
    /// <param name="hash">Hash to match</param>
    /// <returns>Existence</returns>
    Task<bool> ExistsForEd2kHashAsync(byte[] hash);
    
    /// <summary>
    /// Get file that matches the path
    /// </summary>
    /// <param name="path">Path to match</param>
    /// <returns>File if it exists, otherwise default</returns>
    LocalFile? GetForPath(string path);
    
    /// <summary>
    /// Get file that matches the path
    /// </summary>
    /// <param name="path">Path to match</param>
    /// <returns>File if it exists, otherwise default</returns>
    Task<LocalFile?> GetForPathAsync(string path);
    
    /// <summary>
    /// Check if any files exists that match the path
    /// </summary>
    /// <param name="path">Path to match</param>
    /// <returns>Existence</returns>
    bool ExistsForPath(string path);
    
    /// <summary>
    /// Check if any files exists that match the path
    /// </summary>
    /// <param name="path">Path to match</param>
    /// <returns>Existence</returns>
    Task<bool> ExistsForPathAsync(string path);
    
    /// <summary>
    /// Get files without resolution info
    /// </summary>
    /// <returns>Files without resolution info</returns>
    IEnumerable<LocalFile> GetWithoutResolution();
    
    /// <summary>
    /// Get files without resolution info
    /// </summary>
    /// <returns>Files without resolution info</returns>
    IAsyncEnumerable<LocalFile> GetWithoutResolutionAsync();
}
