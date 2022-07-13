using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModularProject.Runtime;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ModularProject.Editor
{
    [InitializeOnLoad]
    internal static class GitDependencesResolver
    {
        private class PackageManifest
        {
            public string[] gitDependencies;
        }
        private static AddRequest _addRequest;
        private static ListRequest _listRequest;
        static GitDependencesResolver()
        {
            // Subscribe to the event using the addition assignment operator (+=).
            Events.registeringPackages += RegisteringPackagesEventHandler;
            Events.registeredPackages += RegisteredPackagesEventHandler;
            //
            EditorApplication.update += Update;
        }

        private static void RegisteringPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            // List packages installed for the project
            _listRequest = Client.List();
            // EditorApplication.update += ListRequestProgress;
            EditorUtility.DisplayProgressBar("List packages installed for the project", "In progress", 0.33f);
        }
        private static void RegisteredPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            // Code executed here can safely assume that the Editor has finished compiling the new list of packages
            foreach (var addedPackage in packageRegistrationEventArgs.added)
            {
                Debug.Log($"Adding {addedPackage.displayName}");
                var packageManifestPath = $"{addedPackage.assetPath}/package.json";
                var packageManifestAsset = (TextAsset)AssetDatabase.LoadAssetAtPath(packageManifestPath, typeof(TextAsset));
                if (null != packageManifestAsset)
                {
                    var packageManifest = JsonUtility.FromJson<PackageManifest>(packageManifestAsset.text);
                    if (null != packageManifest && null != packageManifest.gitDependencies && packageManifest.gitDependencies.Any())
                    {
                        Debug.Log($"Package {addedPackage.displayName} has {packageManifest.gitDependencies.Length} git dependence(s).");
                        // _gitDependencesToAdd.AddRange(packageManifest.gitDependencies.Where(path => !_installedGitDependences.Contains(path)));
                        CacheFile.AddGitDependencesToInstall(packageManifest.gitDependencies);
                        if (null == _listRequest && null == _addRequest) RequestNextGitDependences();
                        // Debug.Log($"Ready to install git dependences, cnt: {_gitDependencestotalCnt}");
                        // EditorUtility.DisplayProgressBar("Ready to install git dependences", "Starting", 0f);
                    }
                }
                else
                {
                    Debug.LogError($"Package manifest file {packageManifestPath} not find.");
                }
            }
        }

        private static void Update()
        {
            if (null != _listRequest && _listRequest.IsCompleted)
            {
                switch (_listRequest.Status)
                {
                    case StatusCode.Success:
                    {
                        var installedGitDependences = new List<string>();
                        foreach (var package in _listRequest.Result)
                        {
                            if (PackageSource.Git != package.source) continue;
                            installedGitDependences.Add(package.repository.url);
                            Debug.Log($"Installed package: [{package.repository.url}]");
                        }
                        CacheFile.SetInstalledPackageList(installedGitDependences);
                        break;
                    }
                    case >= StatusCode.Failure:
                        Debug.Log(_listRequest.Error.message);
                        break;
                }
                EditorUtility.ClearProgressBar();
                _listRequest = null;
            }

            if (null == _listRequest && null != _addRequest && _addRequest.IsCompleted)
            {
                switch (_addRequest.Status)
                {
                    case StatusCode.Success:
                        Debug.Log("Installed: " + _addRequest.Result.packageId);
                        break;
                    case >= StatusCode.Failure:
                        Debug.Log(_addRequest.Error.message);
                        break;
                }
                RequestNextGitDependences();
            }
        }

        private static void RequestNextGitDependences()
        {
            var gitDependence = CacheFile.GetNextGitDependeceToInstall();
            if (string.IsNullOrEmpty(gitDependence)) CleanAddProgress();
            else
            {
                Debug.Log($"Request GitDependence: {gitDependence}");
                _addRequest = Client.Add(gitDependence);
                EditorUtility.DisplayProgressBar($"Request GitDependence: {gitDependence}...", gitDependence, 0.66f);
            }
        }

        private static void CleanAddProgress()
        {
            _addRequest = null;
            EditorUtility.ClearProgressBar();
        }

        private static class CacheFile
        {
            private const string CacheFilePath = "Temp/testCacheFile.txt";
            private const string Seperator = "---I'm Seperator---\n";
            private const string PlaceHolder = "I'm PlaceHolder\n";
            public static void SetInstalledPackageList(List<string> installedList)
            {
                var sb = new StringBuilder();
                if (FileUtils.TryReadFileToString(CacheFilePath, out var cachedData))
                {
                    if (!string.IsNullOrEmpty(cachedData))
                    {
                        var parts = cachedData.Split(Seperator);
                        if (parts.Length == 2)
                        {
                            sb.Append(parts[0]);
                            sb.Append(Seperator);
                            installedList.ForEach(url=>sb.Append($"{url}\n"));
                            FileUtils.WriteStringToFile(sb.ToString(), CacheFilePath);
                            return;
                        }
                        Debug.LogError("Unknown error.");
                    }
                }
                sb.Append(Seperator);
                installedList.ForEach(url=>sb.Append($"{url}\n"));
                FileUtils.WriteStringToFile(sb.ToString(), CacheFilePath);
            }

            private static string[] InstalledPackageList
            {
                get
                {
                    if (FileUtils.TryReadFileToString(CacheFilePath, out var cachedData))
                    {
                        if (!string.IsNullOrEmpty(cachedData))
                        {
                            var parts = cachedData.Split(Seperator);
                            if (parts.Length > 1)
                            {
                                return parts[1].Split('\n');
                            }
                        }
                    }
                    return null;
                }
            }

            public static void AddGitDependencesToInstall(string[] dependences)
            {
                var sb = new StringBuilder();
                foreach (var url in dependences)
                {
                    sb.Append($"{url}\n");
                }
                if (FileUtils.TryReadFileToString(CacheFilePath, out var cachedData))
                {
                    if (!string.IsNullOrEmpty(cachedData))
                    {
                        var parts = cachedData.Split(Seperator);
                        if (parts.Length == 2)
                        {
                            sb.Append(parts[0]);
                            sb.Append(Seperator);
                            sb.Append(parts[1]);
                            FileUtils.WriteStringToFile(sb.ToString(), CacheFilePath);
                            return;
                        }
                        Debug.LogError("Unknown error.");
                    }
                }
                sb.Append(Seperator);
                sb.Append(PlaceHolder);
                FileUtils.WriteStringToFile(sb.ToString(), CacheFilePath);
            }

            public static string GetNextGitDependeceToInstall()
            {
                var sb = new StringBuilder();
                if (FileUtils.TryReadFileToString(CacheFilePath, out var cachedData))
                {
                    if (!string.IsNullOrEmpty(cachedData))
                    {
                        var parts = cachedData.Split(Seperator);
                        if (parts.Length == 2)
                        {
                            var urls = parts[0].Split('\n');
                            var nextUrl = urls[urls.Length - 1];
                            if (string.Compare(nextUrl, PlaceHolder, StringComparison.Ordinal) == 0) return null;
                            for (var i = 0; i < urls.Length - 1; i++)
                            {
                                sb.Append($"{urls[i]}\n");
                            }
                            sb.Append(parts[0]);
                            sb.Append(Seperator);
                            sb.Append(parts[1]);
                            FileUtils.WriteStringToFile(sb.ToString(), CacheFilePath);
                            return nextUrl;
                        }
                        Debug.LogError("Unknown error.");
                        return null;
                    }
                    return null;
                }
                return null;
            }
        }
    }
}