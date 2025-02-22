using UnityEngine;

public class TimerPressurePlate : PressurePlate
{
    [SerializeField] private TimerStartBlock timerStart;
    public override void PressurePlateEvent()
    {
        timerStart.TriggerTimer();
    }
}
