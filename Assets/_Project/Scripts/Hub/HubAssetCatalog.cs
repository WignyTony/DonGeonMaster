using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.Hub
{
    /// <summary>
    /// Catalogue projet-owned des prefabs utilises dans le hub.
    /// Reference uniquement les prefabs reellement places dans la scene HubPrototype.
    /// Les prefabs sources restent dans le pack tiers (EmaceArt).
    /// </summary>
    [CreateAssetMenu(fileName = "HubAssetCatalog", menuName = "DonGeonMaster/Hub/Asset Catalog")]
    public class HubAssetCatalog : ScriptableObject
    {
        [Header("Batiments")]
        public List<GameObject> buildings = new();

        [Header("Props (barils, caisses, bancs, etals...)")]
        public List<GameObject> props = new();

        [Header("Environnement (routes, clotures, escaliers, rochers)")]
        public List<GameObject> environment = new();

        [Header("Nature (arbres, buissons)")]
        public List<GameObject> nature = new();
    }
}
