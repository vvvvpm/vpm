Download(
    "https://vvvv.org/sites/default/files/uploads/VVVV.Audio_AnyCPU_alpha_V11.1.zip",
    VPM.TempDir + "\\VVVV.Audio_AnyCPU_alpha_V11.1.zip"
);
Extract(VPM.TempDir + "\\VVVV.Audio_AnyCPU_alpha_V11.1.zip", Pack.TempDir);
// zip folder structure should be
// packs
//   VVVV.Audio
//     etc...
// so we just copy to vvvv dir directly
CopyDir(Pack.TempDir, VVVV.Dir);
