﻿using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using UnityEngine;

public class DroneTranstructor : MonoBehaviour
{
    [Require]
    private Controller.Writer ControllerWriter;

    [Require]
    private Position.Writer PositionWriter;

    public void CreateDrone(Coordinates position, Vector3f target, float speed, float radius)
    {
        var droneTemplate = EntityTemplateFactory.CreateDroneTemplate(position, target, speed, radius);

        SpatialOS.Commands.CreateEntity(PositionWriter, droneTemplate)
                 .OnSuccess(CreateDroneSuccess);
    }

    void CreateDroneSuccess(Improbable.Unity.Core.EntityQueries.CreateEntityResult response)
    {
        EntityId entityId = response.CreatedEntityId;
        ControllerWriter.Send(new Controller.Update().SetDroneCount(ControllerWriter.Data.droneCount + 1));
    }

    public void DestroyDrone(EntityId entityId)
    {
        SpatialOS.Commands.DeleteEntity(PositionWriter, entityId)
                 .OnSuccess(DestroyDroneSuccess);
    }

    void DestroyDroneSuccess(Improbable.Unity.Core.EntityQueries.DeleteEntityResult response)
    {
        ControllerWriter.Send(new Controller.Update().SetDroneCount(ControllerWriter.Data.droneCount - 1));
    }
}