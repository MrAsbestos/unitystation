using UnityEngine;

public class RackParts : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, ICheckedInteractable<InventoryApply>
{

	public GameObject rackPrefab;
	private bool isBuilding;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		if (Validations.IsTool(interaction.HandObject, ToolType.Wrench))
		{
			return true;
		}

		// Must be constructing the rack somewhere empty
		var vector = interaction.WorldPositionTarget.RoundToInt();
		if (!MatrixManager.IsPassableAt(vector, vector, false))
		{
			return false;
		}

		return true;
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		if (interaction.TargetObject != gameObject
		    || !Validations.IsTool(interaction.HandObject, ToolType.Wrench))
		{
			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (Validations.IsTool(interaction.HandObject, ToolType.Wrench))
		{
			SoundManager.PlayNetworkedAtPos("Wrench", interaction.WorldPositionTarget, 1f);
			ObjectFactory.SpawnMetal(1, interaction.WorldPositionTarget.To2Int(), parent: transform.parent);
			PoolManager.PoolNetworkDestroy(gameObject);

			return;
		}

		if (isBuilding)
		{
			return;
		}

		UpdateChatMessage.Send(interaction.Performer, ChatChannel.Examine,
			"You start constructing a rack...");

		var progressFinishAction = new FinishProgressAction(
			reason =>
			{
				if (reason == FinishProgressAction.FinishReason.INTERRUPTED)
				{
					isBuilding = false;
				}
				else if (reason == FinishProgressAction.FinishReason.COMPLETED)
				{
					UpdateChatMessage.Send(interaction.Performer, ChatChannel.Examine,
						"You assemble a rack.");

					PoolManager.PoolNetworkInstantiate(rackPrefab, interaction.WorldPositionTarget.RoundToInt(), interaction.Performer.transform.parent);

					var handObj = interaction.HandObject;
					var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
					handObj.GetComponent<Pickupable>().DisappearObject(slot);

					isBuilding = false;
				}
			}
		);
		isBuilding = true;

		UIManager.ProgressBar.StartProgress(interaction.WorldPositionTarget.RoundToInt(),
			5f, progressFinishAction, interaction.Performer);
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		SoundManager.PlayNetworkedAtPos("Wrench", interaction.Performer.WorldPosServer(), 1f);
		ObjectFactory.SpawnMetal(1, interaction.Performer.WorldPosServer().To2Int(), parent: transform.parent);

		var rack = interaction.TargetObject;
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.TargetSlot.equipSlot);
		rack.GetComponent<Pickupable>().DisappearObject(slot);
	}
}
