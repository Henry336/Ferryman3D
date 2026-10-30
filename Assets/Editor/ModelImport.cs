using UnityEditor;

// Meshy models: we apply our own toon materials in code, and only the reaper keeps (legacy) animation clips.
public class ModelImport : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Resources/Models/")) return;
        var m = (ModelImporter)assetImporter;
        m.materialImportMode = ModelImporterMaterialImportMode.None;
        bool animated = assetPath.Contains("reaper");
        m.animationType = animated ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
        m.importAnimation = animated;
        m.isReadable = false;
    }
}
