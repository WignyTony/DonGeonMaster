namespace DonGeonMaster.MapGeneration
{
    /// <summary>
    /// Info legere sur le rendu sol d'une cellule, utilisee par AssetPlacer pour enrichir le dump.
    /// Decouple AssetPlacer du type lourd MapStructureDebugRenderer.CellRenderInfo.
    /// </summary>
    public struct CellSupportInfo
    {
        public string renderMode;
        public string materialName;
        public string objectName;
    }
}
