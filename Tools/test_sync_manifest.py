import tempfile
import unittest
from pathlib import Path
from sync_manifest import collect

class ManifestTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.base = Path(self.temp.name)
        self.config = self.base/'ModsConfig.xml'
        self.config.write_text('<ModsConfigData><version>1.6.test</version><activeMods><li>Example.Mod</li></activeMods></ModsConfigData>')
    def tearDown(self):
        self.temp.cleanup()
    def mod(self, location, content):
        root = self.base/location/'mod'
        (root/'About').mkdir(parents=True)
        (root/'About/About.xml').write_text('<ModMetaData><packageId>example.mod</packageId></ModMetaData>')
        (root/'payload.dll').write_bytes(content)
        return root
    def test_missing_mod_blocks(self):
        manifest = collect(self.config, [self.base/'absent'], {})
        self.assertEqual(len(manifest['blockers']), 1)
    def test_different_duplicates_block_and_selected_copy_resolves(self):
        a, b = self.mod('workshop', b'old'), self.mod('local', b'new')
        roots = [a.parent, b.parent]
        self.assertEqual(len(collect(self.config, roots, {})['blockers']), 1)
        chosen = collect(self.config, roots, {'example.mod': str(b)})
        self.assertEqual(chosen['blockers'], [])
        self.assertEqual(len(chosen['packages'][0]['candidates']), 1)
    def test_identical_copies_are_portable_and_do_not_block(self):
        a, b = self.mod('host', b'same'), self.mod('client', b'same')
        host = collect(self.config, [a.parent], {})
        client = collect(self.config, [b.parent], {})
        both = collect(self.config, [a.parent, b.parent], {})
        self.assertEqual(host, client)
        self.assertEqual(host, both)
    def test_selection_cannot_choose_unindexed_directory(self):
        a = self.mod('installed', b'content')
        with self.assertRaises(ValueError):
            collect(self.config, [a.parent], {'example.mod': str(self.base/'other')})

if __name__ == '__main__':
    unittest.main()
