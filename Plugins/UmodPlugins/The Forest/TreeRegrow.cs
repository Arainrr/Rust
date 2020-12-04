﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using TheForest.Utils;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Tree Regrow", "MisterPixie", "1.0.0")]
    [Description("Regrow any cut down tree that still has a stump.")]
    public class TreeRegrow : TheForestPlugin
    {
        private const string _perm = "treeregrow.use";

        [Command("regrowTree"), Permission(_perm)]
        private void cmdRegrowTree(IPlayer player, string command, string[] args)
        {
            List<LOD_Trees> list = (UnityEngine.Object.FindObjectsOfType<LOD_Trees>().Where(t => t.enabled ? false : t.CurrentView == null).ToList());
            if (list != null && list.Count > 0)
            {
                int num1 = 0;
                int totalTrees = 0;
                TreeLodGrid treeLodGrid = UnityEngine.Object.FindObjectOfType<TreeLodGrid>();
                Transform current;
                LOD_Stump lODStump;
                IEnumerator enumerator;
                IDisposable disposable;
                IDisposable disposable1;

                for (int i = 0; i < list.Count; i++)
                {
                    totalTrees++;
                    if (BoltNetwork.isRunning)
                    {
                        CoopTreeId component = list[i].GetComponent<CoopTreeId>();
                        if (component)
                        {
                            component.RegrowTree();
                        }

                        list[i].DontSpawn = false;
                    }

                    list[i].enabled = true;
                    list[i].RefreshLODs();
                    if (treeLodGrid)
                    {
                        treeLodGrid.RegisterTreeRegrowth(list[i].transform.position);
                    }

                    enumerator = list[i].transform.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            current = (Transform) enumerator.Current;
                            lODStump = current.GetComponent<LOD_Stump>();
                            if (lODStump)
                            {
                                lODStump.DespawnCurrent();
                                lODStump.CurrentView = null;
                            }

                            UnityEngine.Object.Destroy(current.gameObject);
                        }
                    }
                    finally
                    {
                        disposable = enumerator as IDisposable;
                        disposable1 = disposable;
                        if (disposable != null)
                        {
                            disposable1.Dispose();
                        }
                    }

                    num1++;
                }

                if (num1 != 0 && BoltNetwork.isRunning)
                {
                    CoopTreeGrid.SweepGrid();
                }

                Puts(string.Concat(new object[] {"Tree regrowth: ", num1, "/", totalTrees}));
            }
        }
    }
}
