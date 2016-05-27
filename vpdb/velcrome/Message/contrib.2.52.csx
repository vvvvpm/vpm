if(VVVV.Architecture == "x64")
{
    Download(
        "https://vvvv.org/sites/default/files/uploads/vvvv-Message_x64_2.7z",
        VPM.TempDir + "\\vvvv-Message.7z"
    );
}
else
{
    Download(
        "https://vvvv.org/sites/default/files/uploads/vvvv-Message_x86_2.7z",
        VPM.TempDir + "\\vvvv-Message.7z"
    );
}
Extract(VPM.TempDir + "\\vvvv-Message.7z", Pack.TempDir);
CopyDir(Pack.TempDir + "\\vvvv-Message_" + VVVV.Architecture, VVVV.Dir + "\\packs\\vvvv-Message");
