using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum StatusBarMode
{
    HIDDEN,
    NOBG,
    WITHBG
}
public class StatusBar : MonoBehaviour
{
    public StatusBarMode mode = StatusBarMode.HIDDEN;
    
    public Image healthBar;
    public Image cooldownBar;
    public Image background;
    public void updateBar(int health, int maxHealth, float cooldown, float maxCooldown)
    {
        healthBar.fillAmount = (float) health / (float) maxHealth;
        cooldownBar.fillAmount = cooldown / maxCooldown;
    }

    public void setStatusBarMode(StatusBarMode newMode)
    {
        mode = newMode;
        if (mode == StatusBarMode.HIDDEN)
        {
            var tempColor = healthBar.color;
            tempColor.a = 0f;
            healthBar.color = tempColor;

            tempColor = cooldownBar.color;
            tempColor.a = 0f;
            cooldownBar.color = tempColor;

            tempColor = background.color;
            tempColor.a = 0f;
            background.color = tempColor;
        }
        else if (mode == StatusBarMode.NOBG)
        {
            var tempColor = healthBar.color;
            tempColor.a = 1f;
            healthBar.color = tempColor;

            tempColor = cooldownBar.color;
            tempColor.a = 1f;
            cooldownBar.color = tempColor;

            tempColor = background.color;
            tempColor.a = 0f;
            background.color = tempColor;
        }
        else if (mode == StatusBarMode.WITHBG)
        {
            var tempColor = healthBar.color;
            tempColor.a = 1f;
            healthBar.color = tempColor;

            tempColor = cooldownBar.color;
            tempColor.a = 1f;
            cooldownBar.color = tempColor;

            tempColor = background.color;
            tempColor.a = 1f;
            background.color = tempColor;
        }
    }


}
