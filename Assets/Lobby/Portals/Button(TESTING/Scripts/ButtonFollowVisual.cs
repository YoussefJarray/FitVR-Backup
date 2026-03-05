using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class ButtonFollowVisual : MonoBehaviour
{

    //button mvm
    public Transform visualTarget;
    private Vector3 offset;

    //button constrain mvm to axis 
    public Vector3 LockAxis;

    //button reset pos to init with smooth transition
    private Vector3 initialPos;
    private float resetSpeed = 5f;

    // poke interactor
    private Transform pokeAttaTransform;
    private XRBaseInteractable interactable;
    private bool isFollowing = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialPos = visualTarget.localPosition;

        interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(FollowVisual);

        interactable.hoverExited.AddListener(ResetButton);
    }

    public void FollowVisual(BaseInteractionEventArgs hover)
    {
        if(hover.interactorObject is XRPokeInteractor)
        {
            XRPokeInteractor pokeInteractor = (XRPokeInteractor)hover.interactorObject;
            isFollowing = true;

            pokeAttaTransform = pokeInteractor.attachTransform;
            offset = visualTarget.position - pokeAttaTransform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(isFollowing)
        {
            Vector3 localTargetPos = visualTarget.InverseTransformPoint(pokeAttaTransform.position + offset);
            Vector3 ConstrainedPos = Vector3.Project(localTargetPos, LockAxis);
            visualTarget.position = visualTarget.TransformPoint(ConstrainedPos);
         }
         else {
            visualTarget.localPosition = Vector3.Lerp(visualTarget.localPosition, initialPos, Time.deltaTime * resetSpeed);
         }

    }



    //reset button 
    public void ResetButton(BaseInteractionEventArgs hover)
    {
       if (hover.interactorObject is XRPokeInteractor)
        {
            isFollowing = false;

            
        }
}
}
