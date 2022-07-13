using System.Linq;
using UnityEngine;

namespace ModularProject.Editor
{
    internal static partial class GitDependencesResolver
    {
        private static void Test()
        {
            PersistentCache.ClearGitDependencesToInstall();
            var strArray = new[]
            {
                "apple",
                "banana"
            };
            PersistentCache.SetInstalledPackageList(strArray.ToList());

            var installed = PersistentCache.InstalledPackageList;
            foreach (var v in installed)
            {
                Debug.Log(v);
            }
            
            var strArray2 = new[]
            {
                "girl",
                "boy",
                "banana"
            };
            PersistentCache.SetInstalledPackageList(strArray2.ToList());

            installed = PersistentCache.InstalledPackageList;
            foreach (var v in installed)
            {
                Debug.Log(v);
            }
            
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            
            var dependeces = new[]
            {
                "monkey",
                "banana",
                "cat"
            };
            PersistentCache.AddGitDependencesToInstall(dependeces);
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            
            PersistentCache.AddGitDependencesToInstall(dependeces);
            PersistentCache.AddGitDependencesToInstall(dependeces);

            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            
            var dependeces2 = new[]
            {
                "panda",
                "monkey",
                "dog"
            };
            
            
            PersistentCache.AddGitDependencesToInstall(dependeces);
            PersistentCache.AddGitDependencesToInstall(dependeces2);
            
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
            Debug.Log($"Next: {PersistentCache.GetNextGitDependeceToInstall()}");
        }
    }
}