﻿/*******************************************************************************
File:      DestroyOnTime.cs
Author:    Victor Cecci
DP Email:  victor.cecci@digipen.edu
Date:      12/5/2016
Course:    CS186
Section:   Z

Description:
    This component is responsible for destroying the game object it is attached
    to when a given amount of time has passed;

*******************************************************************************/
using UnityEngine;

public class DestroyOnTime : MonoBehaviour
{
    public float TimeToDestroy = 2f;

    private float Timer = 0;

    private void Start()
    {
        Pause();
    }
    // Update is called once per frame
    void Update()
    {
        //Increment timer
        Timer += Time.deltaTime;

        //Once the timer reaches the limit, destroy the game object
        if (Timer >= TimeToDestroy)
        {
            UnPause();
            Destroy(gameObject);
        }
    }

    private void Pause()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject enemy in enemies)
        {
            enemy.GetComponent<EnemyChaseLogic>().enabled = false;
            enemy.GetComponent<EnemyShootLogic>().enabled = false;
            enemy.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }

        GameObject Hero = GameObject.FindGameObjectWithTag("Player");
        Hero.GetComponent<TopDownController>().enabled = false;
        Hero.GetComponent<HeroShoot>().enabled = false;
        Hero.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    private void UnPause()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            enemy.GetComponent<EnemyChaseLogic>().enabled = true;
            enemy.GetComponent<EnemyShootLogic>().enabled = true;
            enemy.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }

        GameObject Hero = GameObject.FindGameObjectWithTag("Player");
        Hero.GetComponent<TopDownController>().enabled = true;
        Hero.GetComponent<HeroShoot>().enabled = true;
        Hero.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

}
