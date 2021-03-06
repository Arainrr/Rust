using UnityEngine;

public class XORSwitch : IOEntity
{
	private int input1Amount;

	private int input2Amount;

	public override int GetPassthroughAmount(int outputSlot = 0)
	{
		if (input1Amount > 0 && input2Amount > 0)
		{
			return 0;
		}
		return Mathf.Max(input1Amount, input2Amount);
	}

	public override void UpdateHasPower(int inputAmount, int inputSlot)
	{
		SetFlag(Flags.Reserved8, input1Amount > 0 || input2Amount > 0, false, false);
	}

	public override void IOStateChanged(int inputAmount, int inputSlot)
	{
		base.IOStateChanged(inputAmount, inputSlot);
	}

	public override void UpdateFromInput(int inputAmount, int slot)
	{
		if (inputAmount > 0 && IsConnectedTo(this, slot, IOEntity.backtracking))
		{
			inputAmount = 0;
			SetFlag(Flags.Reserved7, true);
		}
		else
		{
			SetFlag(Flags.Reserved7, false);
		}
		switch (slot)
		{
		case 0:
			input1Amount = inputAmount;
			break;
		case 1:
			input2Amount = inputAmount;
			break;
		}
		int num = (input1Amount <= 0 || input2Amount <= 0) ? Mathf.Max(input1Amount, input2Amount) : 0;
		bool b = num > 0;
		SetFlag(Flags.Reserved1, input1Amount > 0, false, false);
		SetFlag(Flags.Reserved2, input2Amount > 0, false, false);
		SetFlag(Flags.Reserved3, b, false, false);
		SetFlag(Flags.Reserved4, input1Amount > 0 || input2Amount > 0, false, false);
		SetFlag(Flags.On, num > 0);
		base.UpdateFromInput(inputAmount, slot);
	}
}
