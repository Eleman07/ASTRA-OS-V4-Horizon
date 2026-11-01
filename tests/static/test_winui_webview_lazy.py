import unittest
from pathlib import Path
from xml.etree import ElementTree as etree

ROOT=Path(__file__).resolve().parents[2]

class DeferredWebViewTests(unittest.TestCase):
    def test_orbital_webview_not_created_by_xaml(self):
        tree=etree.parse(str(ROOT/'src/Astra.Desktop/MainWindow.xaml'))
        names=[(e.tag.rsplit('}',1)[-1],e.get('{http://schemas.microsoft.com/winfx/2006/xaml}Name')) for e in tree.iter()]
        self.assertIn(('Grid', 'OrbitViewHost'), names)
        self.assertNotIn(('WebView2','OrbitView'), names)
    def test_orbital_on_demand(self):
        code=(ROOT/'src/Astra.Desktop/MainWindow.Extensions.cs').read_text(encoding='utf-8')
        self.assertIn('private WebView2? _orbitView;', code)
        self.assertIn('OrbitViewHost.Children.Add(_orbitView)', code)
        self.assertIn('await view.EnsureCoreWebView2Async(environment)', code)
        self.assertIn('Path.Combine(AstraPaths.UserDataRoot, "WebView2")', code)
        self.assertNotIn('OrbitView.CoreWebView2', code)

if __name__=='__main__': unittest.main()
