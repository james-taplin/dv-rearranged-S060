using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Original WAV assets in the existing Shay bundle; no native game audio copied.
public static class ShayExhaustBuild
{
    public static string[] Import(string source)
    {
        const string destination = "Assets/ShayIntegration/Audio";
        Directory.CreateDirectory(destination);
        var files = Directory.GetFiles(source, "RS060_Shay_*.wav").OrderBy(p => p).ToArray();
        if (files.Length != 60) throw new InvalidOperationException("Generate the 60 original Shay exhaust clips first.");
        return files.Select(file =>
        {
            string path = destination + "/" + Path.GetFileName(file);
            File.Copy(file, path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.preloadAudioData = true;
            importer.loadInBackground = false;
            var serialized = new SerializedObject(importer);
            var normalize = serialized.FindProperty("m_Normalize");
            if (normalize != null) { normalize.boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo(); }
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null || clip.channels != 1 || clip.frequency != 24000)
                throw new InvalidOperationException("Invalid original Shay audio import: " + path);
            return path;
        }).ToArray();
    }
}
