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

using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[TestClass]
public class SimpleAnalysisTests
{
    private static FileAnalysisTestsRunner sharedFileAnalysisTestsRunner;

    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context) => sharedFileAnalysisTestsRunner = await FileAnalysisTestsRunner.CreateInstance(nameof(SimpleAnalysisTests));

    [ClassCleanup]
    public static void ClassCleanup() => sharedFileAnalysisTestsRunner.Dispose();

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_JavaScriptAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.JavaScriptIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_JavaScriptAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.JavaScriptIssues, true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_SecretsAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.SecretsIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_SecretsAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.SecretsIssues, true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_TypeScriptAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.TypeScriptIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_TypeScriptAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.TypeScriptIssues, true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_TypeScriptWithBomAnalysisProducesExpectedIssues() =>
        DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.TypeScriptWithBom, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_TypeScriptWithBomAnalysisProducesExpectedIssues() =>
        DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.TypeScriptWithBom, true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_CFamilyAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.CFamilyIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_CFamilyAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.CFamilyIssues, true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_CssAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.CssIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_CssProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.CssIssues, true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_CssAnalysisInVueProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.VueIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_CssAnalysisInVyeProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.VueIssues, true);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromDisk_HtmlAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.HtmlIssues, false);

    [TestMethod]
    public Task DefaultRuleConfig_ContentFromRpc_HtmlAnalysisProducesExpectedIssues() => DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(FileAnalysisTestsRunner.HtmlIssues, true);

    private async Task DefaultRuleConfig_AnalysisProducesExpectedIssuesInFile(ITestingFile testingFile, bool sendContent)
    {
        var issuesByFileUri = await sharedFileAnalysisTestsRunner.RunAnalysisOnOpenFile(
            testingFile,
            TestContext.TestName,
            sendContent: sendContent,
            compilationDatabasePath: (testingFile as ITestingCFamily)?.GetCompilationDatabasePath());

        issuesByFileUri.Should().HaveCount(1);
        var receivedIssues = issuesByFileUri[new FileUri(testingFile.GetFullPath())];
        var receivedTestIssues = receivedIssues.Select(x => new TestIssue(x.ruleKey, x.textRange, x.severityMode.Right?.cleanCodeAttribute, x.flows.Count));
        receivedTestIssues.Should().BeEquivalentTo(testingFile.ExpectedIssues);
    }
}
