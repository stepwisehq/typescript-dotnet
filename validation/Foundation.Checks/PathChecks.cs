// Copyright (c) Microsoft Corporation. All rights reserved.
// Apache-2.0: licenses/TypeScript-LICENSE.txt.
// Original assertions from pinned tsc/internal/tspath/path_test.go.
// Upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
using TypeScript.Scanner;

namespace TypeScript.Foundation;
public static partial class ScannerChecks
{
    private static void AddPathChecks(Dictionary<string, Action> cases)
    {
        cases.Add("TestNormalizeSlashes/0", () => Equal("a", TsPath.NormalizeSlashes("a"), "TestNormalizeSlashes original assertion 0"));
        cases.Add("TestNormalizeSlashes/1", () => Equal("a/b", TsPath.NormalizeSlashes("a/b"), "TestNormalizeSlashes original assertion 1"));
        cases.Add("TestNormalizeSlashes/2", () => Equal("a/b", TsPath.NormalizeSlashes("a\\b"), "TestNormalizeSlashes original assertion 2"));
        cases.Add("TestNormalizeSlashes/3", () => Equal("//server/path", TsPath.NormalizeSlashes("\\\\server\\path"), "TestNormalizeSlashes original assertion 3"));
        cases.Add("TestGetRootLength/0", () => Equal(0, TsPath.GetRootLength("a"), "TestGetRootLength original assertion 0"));
        cases.Add("TestGetRootLength/1", () => Equal(1, TsPath.GetRootLength("/"), "TestGetRootLength original assertion 1"));
        cases.Add("TestGetRootLength/2", () => Equal(1, TsPath.GetRootLength("/path"), "TestGetRootLength original assertion 2"));
        cases.Add("TestGetRootLength/3", () => Equal(2, TsPath.GetRootLength("c:"), "TestGetRootLength original assertion 3"));
        cases.Add("TestGetRootLength/4", () => Equal(0, TsPath.GetRootLength("c:d"), "TestGetRootLength original assertion 4"));
        cases.Add("TestGetRootLength/5", () => Equal(3, TsPath.GetRootLength("c:/"), "TestGetRootLength original assertion 5"));
        cases.Add("TestGetRootLength/6", () => Equal(3, TsPath.GetRootLength("c:\\"), "TestGetRootLength original assertion 6"));
        cases.Add("TestGetRootLength/7", () => Equal(8, TsPath.GetRootLength("//server"), "TestGetRootLength original assertion 7"));
        cases.Add("TestGetRootLength/8", () => Equal(9, TsPath.GetRootLength("//server/share"), "TestGetRootLength original assertion 8"));
        cases.Add("TestGetRootLength/9", () => Equal(8, TsPath.GetRootLength("\\\\server"), "TestGetRootLength original assertion 9"));
        cases.Add("TestGetRootLength/10", () => Equal(9, TsPath.GetRootLength("\\\\server\\share"), "TestGetRootLength original assertion 10"));
        cases.Add("TestGetRootLength/11", () => Equal(8, TsPath.GetRootLength("file:///"), "TestGetRootLength original assertion 11"));
        cases.Add("TestGetRootLength/12", () => Equal(8, TsPath.GetRootLength("file:///path"), "TestGetRootLength original assertion 12"));
        cases.Add("TestGetRootLength/13", () => Equal(10, TsPath.GetRootLength("file:///c:"), "TestGetRootLength original assertion 13"));
        cases.Add("TestGetRootLength/14", () => Equal(8, TsPath.GetRootLength("file:///c:d"), "TestGetRootLength original assertion 14"));
        cases.Add("TestGetRootLength/15", () => Equal(11, TsPath.GetRootLength("file:///c:/path"), "TestGetRootLength original assertion 15"));
        cases.Add("TestGetRootLength/16", () => Equal(12, TsPath.GetRootLength("file:///c%3a"), "TestGetRootLength original assertion 16"));
        cases.Add("TestGetRootLength/17", () => Equal(8, TsPath.GetRootLength("file:///c%3ad"), "TestGetRootLength original assertion 17"));
        cases.Add("TestGetRootLength/18", () => Equal(13, TsPath.GetRootLength("file:///c%3a/path"), "TestGetRootLength original assertion 18"));
        cases.Add("TestGetRootLength/19", () => Equal(12, TsPath.GetRootLength("file:///c%3A"), "TestGetRootLength original assertion 19"));
        cases.Add("TestGetRootLength/20", () => Equal(8, TsPath.GetRootLength("file:///c%3Ad"), "TestGetRootLength original assertion 20"));
        cases.Add("TestGetRootLength/21", () => Equal(13, TsPath.GetRootLength("file:///c%3A/path"), "TestGetRootLength original assertion 21"));
        cases.Add("TestGetRootLength/22", () => Equal(16, TsPath.GetRootLength("file://localhost"), "TestGetRootLength original assertion 22"));
        cases.Add("TestGetRootLength/23", () => Equal(17, TsPath.GetRootLength("file://localhost/"), "TestGetRootLength original assertion 23"));
        cases.Add("TestGetRootLength/24", () => Equal(17, TsPath.GetRootLength("file://localhost/path"), "TestGetRootLength original assertion 24"));
        cases.Add("TestGetRootLength/25", () => Equal(19, TsPath.GetRootLength("file://localhost/c:"), "TestGetRootLength original assertion 25"));
        cases.Add("TestGetRootLength/26", () => Equal(17, TsPath.GetRootLength("file://localhost/c:d"), "TestGetRootLength original assertion 26"));
        cases.Add("TestGetRootLength/27", () => Equal(20, TsPath.GetRootLength("file://localhost/c:/path"), "TestGetRootLength original assertion 27"));
        cases.Add("TestGetRootLength/28", () => Equal(21, TsPath.GetRootLength("file://localhost/c%3a"), "TestGetRootLength original assertion 28"));
        cases.Add("TestGetRootLength/29", () => Equal(17, TsPath.GetRootLength("file://localhost/c%3ad"), "TestGetRootLength original assertion 29"));
        cases.Add("TestGetRootLength/30", () => Equal(22, TsPath.GetRootLength("file://localhost/c%3a/path"), "TestGetRootLength original assertion 30"));
        cases.Add("TestGetRootLength/31", () => Equal(21, TsPath.GetRootLength("file://localhost/c%3A"), "TestGetRootLength original assertion 31"));
        cases.Add("TestGetRootLength/32", () => Equal(17, TsPath.GetRootLength("file://localhost/c%3Ad"), "TestGetRootLength original assertion 32"));
        cases.Add("TestGetRootLength/33", () => Equal(22, TsPath.GetRootLength("file://localhost/c%3A/path"), "TestGetRootLength original assertion 33"));
        cases.Add("TestGetRootLength/34", () => Equal(13, TsPath.GetRootLength("file://server"), "TestGetRootLength original assertion 34"));
        cases.Add("TestGetRootLength/35", () => Equal(14, TsPath.GetRootLength("file://server/"), "TestGetRootLength original assertion 35"));
        cases.Add("TestGetRootLength/36", () => Equal(14, TsPath.GetRootLength("file://server/path"), "TestGetRootLength original assertion 36"));
        cases.Add("TestGetRootLength/37", () => Equal(14, TsPath.GetRootLength("file://server/c:"), "TestGetRootLength original assertion 37"));
        cases.Add("TestGetRootLength/38", () => Equal(14, TsPath.GetRootLength("file://server/c:d"), "TestGetRootLength original assertion 38"));
        cases.Add("TestGetRootLength/39", () => Equal(14, TsPath.GetRootLength("file://server/c:/d"), "TestGetRootLength original assertion 39"));
        cases.Add("TestGetRootLength/40", () => Equal(14, TsPath.GetRootLength("file://server/c%3a"), "TestGetRootLength original assertion 40"));
        cases.Add("TestGetRootLength/41", () => Equal(14, TsPath.GetRootLength("file://server/c%3ad"), "TestGetRootLength original assertion 41"));
        cases.Add("TestGetRootLength/42", () => Equal(14, TsPath.GetRootLength("file://server/c%3a/d"), "TestGetRootLength original assertion 42"));
        cases.Add("TestGetRootLength/43", () => Equal(14, TsPath.GetRootLength("file://server/c%3A"), "TestGetRootLength original assertion 43"));
        cases.Add("TestGetRootLength/44", () => Equal(14, TsPath.GetRootLength("file://server/c%3Ad"), "TestGetRootLength original assertion 44"));
        cases.Add("TestGetRootLength/45", () => Equal(14, TsPath.GetRootLength("file://server/c%3A/d"), "TestGetRootLength original assertion 45"));
        cases.Add("TestGetRootLength/46", () => Equal(13, TsPath.GetRootLength("http://server"), "TestGetRootLength original assertion 46"));
        cases.Add("TestGetRootLength/47", () => Equal(14, TsPath.GetRootLength("http://server/path"), "TestGetRootLength original assertion 47"));
        cases.Add("TestIsRootedDiskPath/0", () => Equal(false, TsPath.IsRootedDiskPath("a"), "TestIsRootedDiskPath original assertion 0"));
        cases.Add("TestIsRootedDiskPath/1", () => Equal(true, TsPath.IsRootedDiskPath("/"), "TestIsRootedDiskPath original assertion 1"));
        cases.Add("TestIsRootedDiskPath/2", () => Equal(true, TsPath.IsRootedDiskPath("c:"), "TestIsRootedDiskPath original assertion 2"));
        cases.Add("TestIsRootedDiskPath/3", () => Equal(false, TsPath.IsRootedDiskPath("c:d"), "TestIsRootedDiskPath original assertion 3"));
        cases.Add("TestIsRootedDiskPath/4", () => Equal(true, TsPath.IsRootedDiskPath("c:/"), "TestIsRootedDiskPath original assertion 4"));
        cases.Add("TestIsRootedDiskPath/5", () => Equal(true, TsPath.IsRootedDiskPath("c:\\"), "TestIsRootedDiskPath original assertion 5"));
        cases.Add("TestIsRootedDiskPath/6", () => Equal(true, TsPath.IsRootedDiskPath("//server"), "TestIsRootedDiskPath original assertion 6"));
        cases.Add("TestIsRootedDiskPath/7", () => Equal(true, TsPath.IsRootedDiskPath("//server/share"), "TestIsRootedDiskPath original assertion 7"));
        cases.Add("TestIsRootedDiskPath/8", () => Equal(true, TsPath.IsRootedDiskPath("\\\\server"), "TestIsRootedDiskPath original assertion 8"));
        cases.Add("TestIsRootedDiskPath/9", () => Equal(true, TsPath.IsRootedDiskPath("\\\\server\\share"), "TestIsRootedDiskPath original assertion 9"));
        cases.Add("TestIsRootedDiskPath/10", () => Equal(false, TsPath.IsRootedDiskPath("file:///path"), "TestIsRootedDiskPath original assertion 10"));
        cases.Add("TestIsRootedDiskPath/11", () => Equal(false, TsPath.IsRootedDiskPath("file:///c:"), "TestIsRootedDiskPath original assertion 11"));
        cases.Add("TestIsRootedDiskPath/12", () => Equal(false, TsPath.IsRootedDiskPath("file:///c:d"), "TestIsRootedDiskPath original assertion 12"));
        cases.Add("TestIsRootedDiskPath/13", () => Equal(false, TsPath.IsRootedDiskPath("file:///c:/path"), "TestIsRootedDiskPath original assertion 13"));
        cases.Add("TestIsRootedDiskPath/14", () => Equal(false, TsPath.IsRootedDiskPath("file://server"), "TestIsRootedDiskPath original assertion 14"));
        cases.Add("TestIsRootedDiskPath/15", () => Equal(false, TsPath.IsRootedDiskPath("file://server/path"), "TestIsRootedDiskPath original assertion 15"));
        cases.Add("TestIsRootedDiskPath/16", () => Equal(false, TsPath.IsRootedDiskPath("http://server"), "TestIsRootedDiskPath original assertion 16"));
        cases.Add("TestIsRootedDiskPath/17", () => Equal(false, TsPath.IsRootedDiskPath("http://server/path"), "TestIsRootedDiskPath original assertion 17"));
        cases.Add("TestPathIsRelative/0", () => Equal(true, TsPath.PathIsRelative("."), "TestPathIsRelative original assertion 0"));
        cases.Add("TestPathIsRelative/1", () => Equal(true, TsPath.PathIsRelative(".."), "TestPathIsRelative original assertion 1"));
        cases.Add("TestPathIsRelative/2", () => Equal(true, TsPath.PathIsRelative("./"), "TestPathIsRelative original assertion 2"));
        cases.Add("TestPathIsRelative/3", () => Equal(true, TsPath.PathIsRelative("../"), "TestPathIsRelative original assertion 3"));
        cases.Add("TestPathIsRelative/4", () => Equal(true, TsPath.PathIsRelative("./foo/bar"), "TestPathIsRelative original assertion 4"));
        cases.Add("TestPathIsRelative/5", () => Equal(true, TsPath.PathIsRelative("../foo/bar"), "TestPathIsRelative original assertion 5"));
        cases.Add("TestPathIsRelative/6", () => Equal(true, TsPath.PathIsRelative("../foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/foo/"), "TestPathIsRelative original assertion 6"));
        cases.Add("TestPathIsRelative/7", () => Equal(false, TsPath.PathIsRelative(""), "TestPathIsRelative original assertion 7"));
        cases.Add("TestPathIsRelative/8", () => Equal(false, TsPath.PathIsRelative("foo"), "TestPathIsRelative original assertion 8"));
        cases.Add("TestPathIsRelative/9", () => Equal(false, TsPath.PathIsRelative("foo/bar"), "TestPathIsRelative original assertion 9"));
        cases.Add("TestPathIsRelative/10", () => Equal(false, TsPath.PathIsRelative("/foo/bar"), "TestPathIsRelative original assertion 10"));
        cases.Add("TestPathIsRelative/11", () => Equal(false, TsPath.PathIsRelative("c:/foo/bar"), "TestPathIsRelative original assertion 11"));
        cases.Add("TestPathIsRelative/12", () => Equal(true, TsPath.PathIsRelative("."), "TestPathIsRelative original assertion 12"));
        cases.Add("TestPathIsRelative/13", () => Equal(true, TsPath.PathIsRelative(".."), "TestPathIsRelative original assertion 13"));
        cases.Add("TestPathIsRelative/14", () => Equal(true, TsPath.PathIsRelative(".\\"), "TestPathIsRelative original assertion 14"));
        cases.Add("TestPathIsRelative/15", () => Equal(true, TsPath.PathIsRelative("..\\"), "TestPathIsRelative original assertion 15"));
        cases.Add("TestPathIsRelative/16", () => Equal(true, TsPath.PathIsRelative(".\\foo\\bar"), "TestPathIsRelative original assertion 16"));
        cases.Add("TestPathIsRelative/17", () => Equal(true, TsPath.PathIsRelative("..\\foo\\bar"), "TestPathIsRelative original assertion 17"));
        cases.Add("TestPathIsRelative/18", () => Equal(true, TsPath.PathIsRelative("..\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\foo\\"), "TestPathIsRelative original assertion 18"));
        cases.Add("TestPathIsRelative/19", () => Equal(false, TsPath.PathIsRelative(""), "TestPathIsRelative original assertion 19"));
        cases.Add("TestPathIsRelative/20", () => Equal(false, TsPath.PathIsRelative("foo"), "TestPathIsRelative original assertion 20"));
        cases.Add("TestPathIsRelative/21", () => Equal(false, TsPath.PathIsRelative("foo\\bar"), "TestPathIsRelative original assertion 21"));
        cases.Add("TestPathIsRelative/22", () => Equal(false, TsPath.PathIsRelative("\\foo\\bar"), "TestPathIsRelative original assertion 22"));
        cases.Add("TestPathIsRelative/23", () => Equal(false, TsPath.PathIsRelative("c:\\foo\\bar"), "TestPathIsRelative original assertion 23"));
    }
}
