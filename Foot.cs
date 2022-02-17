using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Foot : MonoBehaviour
{
    [SerializeField] 
    GameObject Player;
    [SerializeField]
    GameObject MainCamera;
    Player player;

    Vector3 cameraNormalPos;
    Vector3 cameraDownPos;

    float Aspeed = 4f;
    float Bspeed = 1f;
    float cameraDownDistance = 0.2f;
    float sumTime = 0f;
    bool landFlag = false;
    int Status = 0;
    // Start is called before the first frame update
    void Start()
    {
        player = Player.GetComponent<Player>();
        cameraNormalPos = MainCamera.transform.localPosition;
        cameraDownPos = new Vector3(cameraNormalPos.x, cameraNormalPos.y - cameraDownDistance, cameraNormalPos.z);
    }

    // Update is called once per frame
    void Update()
    {
        if (landFlag)
        {
            if (Status == 0)
            {
                sumTime += Time.deltaTime;
                MainCamera.transform.localPosition = Vector3.Lerp(cameraNormalPos, cameraDownPos, (sumTime * Aspeed) / cameraDownDistance);
                if ((sumTime * Aspeed) / cameraDownDistance >= 1)
                {
                    Status = 1;
                    sumTime = 0f;
                }

            }
            if (Status == 1)
            {
                sumTime += Time.deltaTime;
                MainCamera.transform.localPosition = Vector3.Lerp(cameraDownPos, cameraNormalPos, (sumTime * Bspeed) / cameraDownDistance); 
                if ((sumTime * Bspeed) / cameraDownDistance >= 1)
                {
                    Status = 2;
                    landFlag = false;
                    sumTime = 0f;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        landFlag = true;
        Status = 0;
        sumTime = 0f;
        player.SetJumpStatusFlag();
    }
}
