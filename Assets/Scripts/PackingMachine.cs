using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class PackingMachine : ProcessingMachine
{
    public PalletBoxSpawner palletBoxSpawner;

    protected override void OutputProduct()
    {
        palletBoxSpawner.SpawnObject(inputtedProductCodes);
    }

    protected override bool OutputReady()
    {
        return palletBoxSpawner.ReadyToSpawn();
    }
}