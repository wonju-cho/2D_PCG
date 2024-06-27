/*******************************************************************************
File:      TopDownController.cs
Author:    Victor Cecci
DP Email:  victor.cecci@digipen.edu
Date:      12/5/2016
Course:    CS186
Section:   Z

Description:
    This component is responsible for all the movement actions for a top down
    character.

*******************************************************************************/
using UnityEngine;
using System.Collections;
using UnityEngine.UI;


public class TopDownController : MonoBehaviour
{
    //Private References
    private Rigidbody2D RB;
    private HeroStats HS;

    private float Timer = 0f;
    
    private float DashAmount = 2.0f;
   
    public Image DashAbilityImage;
    public Image DashAbilityBoxImage;
    public Image ProgressBar;
    public Text  Space;

    [SerializeField] private float DashCooldown = 2.0f;

    private bool boxCooldown = false;

    private bool Once = false;


    // Start is called before the first frame update
    void Start()
    {
        DashAbilityImage.enabled = false;
        DashAbilityBoxImage.enabled = false;
        ProgressBar.enabled = false;
        Space.enabled = false;

        RB = GetComponent<Rigidbody2D>();
        HS = GetComponent<HeroStats>();

        if (HS == null)
            Debug.Log("no HS");
    }

    // Update is called once per frame
    void Update()
    {
        Timer += Time.deltaTime;
        
        //Reset direction every frame
        Vector3 dir = Vector3.zero;

        //Determine movement direction based on input
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            dir += Vector3.up;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            dir += Vector3.left;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            dir += Vector3.down;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            dir += Vector3.right;
        if (Input.GetKey(KeyCode.Z))
            HS.ResetLevel();
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();

        if(HS.GetDashAbility() && !Once)
        {
            DashAbilityImage.enabled = true;
            DashAbilityBoxImage.enabled = true;
            Space.enabled = true;
            ProgressBar.enabled = true;
            DashAbilityBoxImage.fillAmount = 0;

            Once = true;
        }

        if (HS.GetDashAbility() && Input.GetKey(KeyCode.Space))
        {
            boxCooldown = true;
                       
            if (DashAbilityBoxImage.fillAmount >= 1)
            {
                DashAbilityBoxImage.fillAmount = 0;
                boxCooldown = false;
            }
            
            if (Timer >= DashCooldown)
            {    
                RB.MovePosition(transform.position + dir * DashAmount);
                Timer = 0;  
            }
        }

        if(boxCooldown)
        {
            DashAbilityBoxImage.fillAmount += (1 / DashCooldown) * Time.deltaTime;
            if (DashAbilityBoxImage.fillAmount >= 1)
            {
                DashAbilityBoxImage.fillAmount = 0;
                boxCooldown = false;
            }
        }

        //Apply velocity
        RB.velocity = dir.normalized * GetComponent<HeroStats>().Speed;
    }
}
