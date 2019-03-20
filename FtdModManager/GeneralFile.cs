using System.IO;
using BrilliantSkies.Core.FilesAndFolders;

namespace FtdModManager
{
    public class GeneralFile : BaseFile
    {
        public string FileNameWithExtension { get; private set; }

        public GeneralFile(IFileSource source) : base(source)
        {
            FileNameWithExtension = Path.GetFileName(source.FilePath);
        }

        public void CopyTo(GeneralFolder folder)
        {
            var newFile = folder.CreateFile(FileName);
            CopyTo(newFile);
        }

        public void CopyTo(string folderPath)
        {
            Directory.CreateDirectory(folderPath);
            CopyDir.CopyAll(new DirectoryInfo(TempGetFileFolder()), new DirectoryInfo(folderPath));
        }

        public void CopyTo(GeneralFile newFile)
        {
            Directory.CreateDirectory(newFile.TempGetFileFolder());
            CopyDir.CopyAll(new DirectoryInfo(TempGetFileFolder()), new DirectoryInfo(newFile.TempGetFileFolder()));
        }
    }
}
