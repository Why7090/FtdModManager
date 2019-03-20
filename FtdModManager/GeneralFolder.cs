using BrilliantSkies.Core.FilesAndFolders;

namespace FtdModManager
{
    public class GeneralFolder : BaseFolder<GeneralFile>
    {
        protected override string FileExtension => "*";

        public GeneralFolder(IFolderSource source, bool forceReadOnly = false) : base(source, forceReadOnly)
        {
        }

        protected override GeneralFile MakeFile(IFileSource path)
        {
            return new GeneralFile(path);
        }

        protected override BaseFolder<GeneralFile> MakeAnotherOfUs(IFolderSource folder)
        {
            return new GeneralFolder(folder, false);
        }
    }
}
