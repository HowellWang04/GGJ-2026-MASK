using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using UnityEngine;

using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [Header("Level 1 Tasks Scriptable Object")]
    public PlayerTask_SO Task_SO;

    private void Start()
    {
        Task_SO.Task[0].IsAlarm = true;
        Task_SO.Task[0].Refrigerator = 0;
        Task_SO.Task[0].Stove = 0;
        Task_SO.Task[0].Desk = 0;
        Task_SO.Task[0].Child = 0;
        Task_SO.Task[0].Book = 0;
        Task_SO.Task[0].Bag = 0;
        Task_SO.Task[0].Window = 0;
        Task_SO.Task[0].Robot = 0;
        Task_SO.Task[0].Clothes = 0;
        Task_SO.Task[0].Wardrobe = 0;
    }
}
