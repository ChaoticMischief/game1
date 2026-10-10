"""Tests for tools/ci/check_upstream.py's version logic (no network).

Run: python3 -m unittest discover -s tests/ci -v
"""
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools" / "ci"))

import check_upstream as cu  # noqa: E402


class VersionKeys(unittest.TestCase):
    def test_unity_versions(self):
        self.assertEqual(cu.unity_version_key("6000.6.3f1"), (6000, 6, 3, "f", 1))
        self.assertLess(cu.unity_version_key("6000.6.3f1"), cu.unity_version_key("6000.6.10f1"))
        self.assertIsNone(cu.unity_version_key("6000.6"))

    def test_semver_skips_prereleases(self):
        self.assertEqual(cu.semver_key("v10.3.0"), (10, 3, 0))
        self.assertIsNone(cu.semver_key("1.0.0-preview.1"))


class EditorUpdates(unittest.TestCase):
    RELEASES = ["6000.6.5f1", "6000.6.4f1", "6000.3.26f1", "6000.7.0b2", "6000.6.3f1"]

    def test_patch_on_our_line(self):
        self.assertEqual(cu.editor_updates("6000.6.3f1", self.RELEASES), ("6000.6.5f1", None))

    def test_newer_line_ignores_betas_and_older_lines(self):
        self.assertEqual(cu.editor_updates("6000.6.5f1", self.RELEASES), (None, None))
        self.assertEqual(cu.editor_updates("6000.6.5f1", self.RELEASES + ["7000.0.0f1"]), (None, "7000.0.0f1"))


class RegistryUpdates(unittest.TestCase):
    VERSIONS = {
        "1.20.0": {"unity": "6000.0"},
        "1.20.1": {"unity": "6000.0"},
        "1.21.0-pre.1": {"unity": "6000.0"},
        "1.22.0": {"unity": "6000.8"},  # needs a newer editor
    }

    def test_newest_compatible_stable(self):
        self.assertEqual(cu.compatible_registry_update("1.20.0", self.VERSIONS, "6000.6.3f1"), "1.20.1")

    def test_newer_editor_unlocks_more(self):
        self.assertEqual(cu.compatible_registry_update("1.20.0", self.VERSIONS, "6000.8.0f1"), "1.22.0")

    def test_up_to_date_or_bundled(self):
        self.assertIsNone(cu.compatible_registry_update("1.20.1", self.VERSIONS, "6000.6.3f1"))
        # Bundled packages (URP) are newer than anything in the registry.
        self.assertIsNone(cu.compatible_registry_update("17.6.0", {"10.10.1": {"unity": "2020.3"}}, "6000.6.3f1"))


class Parsing(unittest.TestCase):
    def test_git_package(self):
        self.assertEqual(cu.git_package("https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0"),
                         ("CoplayDev/unity-mcp", "v10.0.0"))
        self.assertEqual(cu.git_package("https://github.com/getsentry/unity.git#4.10.3"), ("getsentry/unity", "4.10.3"))
        self.assertIsNone(cu.git_package("https://github.com/owner/repo.git#main"))  # not a version tag

    def test_analyzers_readme(self):
        text = ("`Microsoft.Unity.Analyzers.dll` is [Microsoft.Unity.Analyzers](https://github.com/x)\n"
                "1.27.0 (MIT), from the NuGet package")
        self.assertEqual(cu.analyzers_version(text), "1.27.0")

    def test_markdown(self):
        body = cu.markdown([("com.x", "1.0.0", "1.1.0", "Unity registry")], ["y: timeout"])
        self.assertIn("| `com.x` | 1.0.0 | **1.1.0** | Unity registry |", body)
        self.assertIn("- y: timeout", body)
        self.assertIn("up to date", cu.markdown([], []))


if __name__ == "__main__":
    unittest.main()
