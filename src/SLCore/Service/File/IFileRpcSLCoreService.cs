﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.Service.File;

[JsonRpcClass("file")]
public interface IFileRpcSLCoreService : ISLCoreService
{
    Task<GetFilesStatusResponse> GetFilesStatusAsync(GetFilesStatusParams parameters);

    void DidUpdateFileSystem(DidUpdateFileSystemParams parameters);

    /// <summary>
    /// Should be called by clients when a file has been opened in the editor.
    /// </summary>
    void DidOpenFile(DidOpenFileParams parameters);

    /// <summary>
    /// Should be called by clients when a file has been closed in the editor.
    /// </summary>
    /// <param name="parameters"></param>
    void DidCloseFile(DidCloseFileParams parameters);
}
