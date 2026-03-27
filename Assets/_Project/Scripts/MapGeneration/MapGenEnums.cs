namespace DonGeonMaster.MapGeneration
{
    public enum CellType
    {
        Vide,
        Sol,
        Couloir,
        Mur,
        Eau
    }

    public enum BiomeType
    {
        Foret,
        ForetAutomne,
        ForetHiver,
        Prairie,
        Desert,
        Marecage,
        Rocailleux,
        Fantaisie
    }

    public enum GenerationMode
    {
        Complet,
        StructureSeule,
        StructureEtDecor,
        StructureEtGameplay,
        SansEnnemis,
        SansProps,
        SalleUnique,
        Enchainement
    }

    public enum GenerationStatus
    {
        Succes,
        SuccesAvecWarnings,
        Echec
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Erreur
    }

    public enum LayoutType
    {
        BSP,
        Aleatoire,
        Lineaire,
        Circulaire
    }

    public enum CameraMode
    {
        VueDEnsemble,
        SuiviJoueur
    }
}
