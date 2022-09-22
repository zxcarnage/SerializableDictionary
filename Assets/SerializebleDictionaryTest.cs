using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class TestDictionary1 : SerializebleDictionary<string, int> { }
[Serializable] public class TestDictionary2 : SerializebleDictionary<Vector2, GameObject> { }



public class SerializebleDictionaryTest : MonoBehaviour
{
    [SerializeField] private TestDictionary1 dictionary1;
    [SerializeField] public TestDictionary2 dictionary2;

}
