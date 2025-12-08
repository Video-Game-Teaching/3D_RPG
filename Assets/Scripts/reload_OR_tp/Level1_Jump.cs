using UnityEngine;
using UnityEngine.SceneManagement;

public class Level1_Jump : MonoBehaviour
{
    public string nextLevelName = "Level2_Demo";
    
    void OnTriggerEnter(Collider other) 
    {
        if(other.GetComponent<CharacterController>()) 
        {
            Debug.Log("Level Complete!");
            
            // 销毁当前玩家，让新关卡使用预设的玩家
            if (PlayerController.Instance != null)
            {
                Destroy(PlayerController.Instance.gameObject);
            }
            
            SceneManager.LoadScene(nextLevelName);
        }
    }
}

