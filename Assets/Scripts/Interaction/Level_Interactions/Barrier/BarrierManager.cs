using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierManager : Singleton<BarrierManager>
{


    [SerializeField] Dictionary<Barrier, List<Key>> m_KeyMap;
    
    List<Key> m_KeyInventory;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}