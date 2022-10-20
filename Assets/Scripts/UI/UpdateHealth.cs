using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateHealth : MonoBehaviour
{
    private Image HealthBar;
    public float CurrentHealth = 100;
    private float MaxHealth;
    FirstPersonController Player;

    void Start()
    {
        HealthBar = GetComponent<Image>();
        Player = FindObjectOfType<FirstPersonController>();
        this.MaxHealth = Player.MaxHealth;
    }

    private void Update()
    {
        CurrentHealth = Player.Health;
        HealthBar.fillAmount = CurrentHealth / MaxHealth;
    }

}
