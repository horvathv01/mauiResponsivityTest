namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    public interface IPanel
    {
        void Close();

        bool IsLathato { get; }

        long Sorrend { get; }

        string Fejlec { get; }
    }
}
