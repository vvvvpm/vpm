GitClone("https://github.com/microdee/vvvv-Message.git", Pack.TempDir);

BuildSolution(2013, Pack.TempDir + "\\src\\vvvv-Message.sln", "Release|" + VVVV.Architecture, true);

CopyDir(
	Pack.TempDir + "\\build\\" + VVVV.Architecture + "\\Release",
	VVVV.Dir + "\\packs\\vvvv-Message"
);
CopyDir(
	Pack.TempDir + "\\build\\AnyCPU\\Release",
	VVVV.Dir + "\\packs\\vvvv-Message"
);
