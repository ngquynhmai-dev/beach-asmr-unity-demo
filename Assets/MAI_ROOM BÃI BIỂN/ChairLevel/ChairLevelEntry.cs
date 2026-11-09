using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mai.Beach
{
    public sealed class ChairLevelEntry : MonoBehaviour
    {
        public static string ReturnScene="MAI_ROOM_BAI_BIEN_DEMO14";
        private GUIStyle _style;
        private void OnGUI()
        {
            if(_style==null)_style=new GUIStyle(GUI.skin.button){fontStyle=FontStyle.Bold,fontSize=18,wordWrap=true};
            float scale=Mathf.Clamp(Screen.width/900f,.7f,2f);
            float width=Mathf.Min(Screen.width-24,340*scale);
            var old=GUI.backgroundColor;GUI.backgroundColor=new Color(.28f,.85f,.75f);
            if(GUI.Button(new Rect((Screen.width-width)*.5f,Screen.height-64*scale,width,48*scale),"Làm sạch ghế · 13 bước",_style))
            {ReturnScene=SceneManager.GetActiveScene().name;SceneManager.LoadScene("MAI_CHAIR_CLEANING_13");}
            GUI.backgroundColor=old;
        }
    }
}
