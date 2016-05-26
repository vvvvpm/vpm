GitClone("https://github.com/mrvux/dx11-vvvv.git", Pack.TempDir);
GitClone("https://github.com/mrvux/FeralTic.git", Pack.TempDir + "\\FeralTic");
GitClone("https://github.com/mrvux/dx11-vvvv-girlpower.git", Pack.TempDir + "\\girlpower");

BuildSolution(2013, Pack.TempDir + "\\vvvv-dx11.sln", "Release|" + VVVV.Architecture);

CopyDir(
	Pack.TempDir + "\\Deploy\\Release\\" + VVVV.Architecture + "\\packs\\dx11",
	VVVV.Dir + "\\packs\\dx11"
);