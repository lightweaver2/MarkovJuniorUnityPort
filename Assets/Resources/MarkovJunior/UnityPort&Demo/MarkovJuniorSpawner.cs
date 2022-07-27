using System.Collections;
using UnityEngine;
using System;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;

// Copyright (C) 2022 Maxim Gumin, The MIT License (MIT)
public class MarkovJuniorSpawner : MonoBehaviour
{
    public float blockSize = 1;
    public int amount = 1;          // 總共要生成幾組地圖，正常來說是一組，或許可以生成多組然後互相比較地圖優劣
    [Range(1,151)]
    public int linearSize = 33;     // 地圖大小
    [Range(2,3)]
    public int dimension = 2;       // 地圖2D還是3D?
    public TextAsset xmlCore;       
    public BlockPalette blockPalette;
    public bool showSpawningProcess = false;    // 顯示生成的過程，可以拿來debug和錄酷酷的影片
    [Range(1,16)]
    public int speed = 4;                       // 生成的速度
    // public FxPlayer fxPlayer;
    private List<Tuple<GameObject, byte>> activeBlocks;
    public IEnumerator Spawn()
    {
        // Stopwatch sw = Stopwatch.StartNew();
        // var folder = System.IO.Directory.CreateDirectory("output");
        // foreach (var file in folder.GetFiles()) file.Delete();

        // Dictionary<char, int> palette = XDocument.Load("resources/palette.xml").Root.Elements("color").ToDictionary(x => x.Get<char>("symbol"), x => (255 << 24) + Convert.ToInt32(x.Get<string>("value"), 16));

        System.Random meta = new();
        // XDocument xModelVariable = XDocument.Parse(modelVariable.ToString());
         
        
        // // foreach (XElement xmodel in xCore.Root.Elements("model"))
        // // {
        // XElement xmodel = xModelVariable.Root.Element("model");
        // string name = xmodel.Get<string>("name");
        // int linearSize = xmodel.Get("size", -1);
        // int dimension = xmodel.Get("d", 2);
        // int MX = xmodel.Get("length", linearSize);
        // int MY = xmodel.Get("width", linearSize);
        // int MZ = xmodel.Get("height", dimension == 2 ? 1 : linearSize);

        // // Console.Write($"{name} > ");
        // string filename = $"Assets/Resources/MarkovJunior/models/{name}.xml";


        // int amount = xmodel.Get("amount", 1);
        // int pixelsize = xmodel.Get("pixelsize", 4);
        // string seedString = xmodel.Get<string>("seeds", null);
        // int[] seeds = seedString?.Split(' ').Select(s => int.Parse(s)).ToArray();
        // bool showSpawningProcess = xmodel.Get("showSpawningProcess", false);
        // bool iso = xmodel.Get("iso", false);
        // int steps = xmodel.Get("steps", showSpawningProcess ? 1000 : 50000);
        // int gui = xmodel.Get("gui", 0);
        // if (showSpawningProcess) amount = 1;

        // Dictionary<char, int> customPalette = new(palette);
        // foreach (var x in xmodel.Elements("color")) customPalette[x.Get<char>("symbol")] = (255 << 24) + Convert.ToInt32(x.Get<string>("value"), 16);
        string seedString = null;
        int[] seeds = seedString?.Split(' ').Select(s => int.Parse(s)).ToArray();
        int MX = linearSize;
        int MY = linearSize;
        int MZ = dimension == 2 ? 1 : linearSize;
        int steps = showSpawningProcess ? 1000 : 50000;
        XDocument xModelCore = XDocument.Parse(xmlCore.ToString());

        activeBlocks = new List<Tuple<GameObject, byte>>(new Tuple<GameObject, byte>[MX * MY * MZ]);

        // Interpreter interpreter = Interpreter.Load(xModelCore.Root, MX, MY, MZ);
        Interpreter interpreter = Interpreter.Load(xModelCore.Root, MX, MY, MZ);
        if (interpreter == null)
        {
            UnityEngine.Debug.LogWarning("ERROR");
            yield return 0;
        }

        for (int k = 0; k < amount; k++)
        {
            int seed = seeds != null && k < seeds.Length ? seeds[k] : meta.Next();
            foreach ((byte[] result, char[] legend, int FX, int FY, int FZ) in interpreter.Run(seed, steps, showSpawningProcess))
            {
                // int[] colors = legend.Select(ch => customPalette[ch]).ToArray();
                // string outputname = showSpawningProcess ? $"output/{interpreter.counter}" : $"output/{name}_{seed}";
                // 2d
                if(FX != MX)
                {
                    blockSize = blockSize * ((float)MX / (float)FX);
                    activeBlocks.ForEach(p => {
                        UnityEngine.Debug.Log(p);
                        if(p.Item1!= null) 
                            Destroy(p.Item1);
                            // p.Item1.ReturnToPool();
                    });
                    activeBlocks = new List<Tuple<GameObject, byte>>(new Tuple<GameObject, byte>[FX * FY * FZ]);
                    MX = FX; MY = FY; MZ = FZ;
                }
                // yield return 0;
                Render(result, FX, FY, FZ, blockSize, blockPalette);
                
                // if (FZ == 1 || iso)
                // {
                //     var (bitmap, WIDTH, HEIGHT) = Graphics.Render(result, FX, FY, FZ, colors, pixelsize, gui);
                //     if (gui > 0) GUI.Draw(name, interpreter.root, interpreter.current, bitmap, WIDTH, HEIGHT, customPalette);
                    // Graphics.SaveBitmap(bitmap, WIDTH, HEIGHT, outputname + ".png");
                //     Graphics.Render(result, FX, FY, FZ, colors, pixelsize, gui);
                // }
                // 3d 
                // else 
                    // VoxHelper.SaveVox(result, (byte)FX, (byte)FY, (byte)FZ, colors, outputname + ".vox");
                yield return new WaitForSeconds(0.5f / speed);
            }
            UnityEngine.Debug.Log("DONE");
        }
        // }
        // UnityEngine.Debug.Log($"time = {sw.ElapsedMilliseconds}");
    }
    

    
    void Render(byte[] state, int MX, int MY, int MZ, float blockSize, BlockPalette blockPalette)
    {
        // activeBlocks.ForEach(p => p.ReturnToPool());
        // activeBlocks.Clear();
        
        for (int z = 0; z < MZ; z++) for (int y = 0; y < MY; y++) for (int x = 0; x < MX; x++)
        {
            int i = x + y * MX + z * MX * MY;
            byte value = state[i];

            if (activeBlocks[i] == null)
            {
                activeBlocks[i] = Tuple.Create<GameObject, byte>(null, 0);
                // UnityEngine.Debug.Log("add new block");
            }

            if (activeBlocks[i].Item2 != value)
            {
                if(activeBlocks[i].Item1 != null)
                    // activeBlocks[i].Item1.ReturnToPool();
                    Destroy(activeBlocks[i].Item1);
                if(value > 0)
                {

                    Vector3 pos = new Vector3(x, z, y) * blockSize;
                    int blockCount = blockPalette.blockList.Count;
                    GameObject newBlock;
                    bool isColor = false;
                    if(value >= blockCount)
                    {
                        UnityEngine.Debug.LogWarning("Value exceed available blocks");
                        return;

                    }
                    else if(blockPalette.blockList[value].b == null)
                    {
                        newBlock = blockPalette.defaultBlock;
                        isColor = true;
                    }
                    else
                        newBlock = blockPalette.blockList[value].b;

                    // fxPlayer.EmitPoolObject = newBlock;

                    // PoolObject p = fxPlayer.PlayCustomObjectResult();
                    GameObject p = Instantiate(newBlock);

                    p.transform.position = pos;
                    p.transform.localScale = Vector3.one * blockSize;
                    if(isColor)
                    {
                        p.GetComponent<MeshRenderer>().material.color = blockPalette.blockList[value].c;
                        activeBlocks[i] = Tuple.Create<GameObject, byte>(p, value);
                    }
                    
                    // voxels[x + y + z].Add(new Voxel(colors[value], x, y, z));
                }
                else
                {
                    activeBlocks[i] = Tuple.Create<GameObject, byte>(null, 0);

                }

            }
            

        }
    }
}
