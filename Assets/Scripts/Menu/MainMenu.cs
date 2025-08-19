using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject menuPointsCase;
    [SerializeField] GameObject changeMapCase;


    public void OpenCloseChangeMapCase()
    {
        if (menuPointsCase != null && changeMapCase != null)
        {
            if (menuPointsCase.activeSelf)
            {
                menuPointsCase.SetActive(false);
                changeMapCase.SetActive(true);
            }
            else
            {
                menuPointsCase.SetActive(true);
                changeMapCase.SetActive(false);
            }
           
        }
    }
    public void MapChange(int mapID)
    {
        if (mapID==1)
        {
            SceneManager.LoadScene("CartoonRaceTrack");
        }
        else
        {
            SceneManager.LoadScene("Night");
        }
    }
}
