Download(
    "https://vvvv.org/sites/default/files/uploads/vvvv-Patchbox_2.7z",
    VPM.TempDir + "\\vvvv-Patchbox_2.7z"
);
Extract(VPM.TempDir + "\\vvvv-Patchbox_2.7z", Pack.TempDir);
CopyDir(Pack.TempDir, VVVV.Dir + "\\packs");
