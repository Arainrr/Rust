using Facepunch;
using Network;
using Oxide.Core;
using ProtoBuf;
using Rust;
using System;
using UnityEngine;

public class PhoneController : EntityComponent<BaseEntity>
{
	private PhoneController activeCallTo;

	public int PhoneNumber;

	public string PhoneName;

	public bool CanModifyPhoneName = true;

	public bool CanSaveNumbers = true;

	public bool RequirePower = true;

	public bool RequireParent;

	public float CallWaitingTime = 12f;

	public bool AppendGridToName;

	public GameObjectRef PhoneDialog;

	public VoiceProcessor VProcessor;

	public SoundDefinition[] AllPreloadedContent;

	public SoundDefinition DialToneSfx;

	public SoundDefinition RingingSfx;

	public SoundDefinition ErrorSfx;

	public SoundDefinition CallIncomingWhileBusySfx;

	public SoundDefinition PickupHandsetSfx;

	public SoundDefinition PutDownHandsetSfx;

	public SoundDefinition FailedWrongNumber;

	public SoundDefinition FailedNoAnswer;

	public SoundDefinition FailedNetworkBusy;

	public SoundDefinition FailedEngaged;

	public Light RingingLight;

	public float RingingLightFrequency = 0.4f;

	public AudioSource answeringMachineSound;

	public EntityRef currentPlayerRef;

	public Telephone.CallState serverState
	{
		get;
		set;
	}

	public BasePlayer currentPlayer
	{
		get
		{
			if (currentPlayerRef.IsValid(isServer))
			{
				return currentPlayerRef.Get(isServer).ToPlayer();
			}
			return null;
		}
		set
		{
			currentPlayerRef.Set(value);
		}
	}

	private bool isServer => base.baseEntity.isServer;

	public int lastDialedNumber
	{
		get;
		set;
	}

	public PhoneDirectory savedNumbers
	{
		get;
		set;
	}

	public BaseEntity ParentEntity => base.baseEntity;

	public void ServerInit()
	{
		if (PhoneNumber == 0 && !Rust.Application.isLoadingSave)
		{
			PhoneNumber = TelephoneManager.GetUnusedTelephoneNumber();
			if (AppendGridToName & !string.IsNullOrEmpty(PhoneName))
			{
				PhoneName = PhoneName + " " + PositionToGridCoord(base.transform.position);
			}
			TelephoneManager.RegisterTelephone(this);
		}
	}

	public void PostServerLoad()
	{
		currentPlayer = null;
		base.baseEntity.SetFlag(BaseEntity.Flags.Busy, false);
		TelephoneManager.RegisterTelephone(this);
	}

	public void DoServerDestroy()
	{
		TelephoneManager.DeregisterTelephone(this);
	}

	public void ClearCurrentUser(BaseEntity.RPCMessage msg)
	{
		ClearCurrentUser();
	}

	public void ClearCurrentUser()
	{
		if (currentPlayer != null)
		{
			currentPlayer.SetActiveTelephone(null);
			currentPlayer = null;
		}
		base.baseEntity.SetFlag(BaseEntity.Flags.Busy, false);
	}

	public void SetCurrentUser(BaseEntity.RPCMessage msg)
	{
		BasePlayer player = msg.player;
		if (!(currentPlayer == player))
		{
			UpdateServerPlayer(player);
			if (serverState == Telephone.CallState.Dialing || serverState == Telephone.CallState.Ringing || serverState == Telephone.CallState.InProcess)
			{
				ServerHangUp(default(BaseEntity.RPCMessage));
			}
		}
	}

	private void UpdateServerPlayer(BasePlayer newPlayer)
	{
		if (!(currentPlayer == newPlayer))
		{
			if (currentPlayer != null)
			{
				currentPlayer.SetActiveTelephone(null);
			}
			currentPlayer = newPlayer;
			base.baseEntity.SetFlag(BaseEntity.Flags.Busy, currentPlayer != null);
			if (currentPlayer != null)
			{
				currentPlayer.SetActiveTelephone(this);
			}
		}
	}

	public void InitiateCall(BaseEntity.RPCMessage msg)
	{
		if (!(msg.player != currentPlayer))
		{
			int number = msg.read.Int32();
			CallPhone(number);
		}
	}

	public void CallPhone(int number)
	{
		if (number == PhoneNumber)
		{
			OnDialFailed(Telephone.DialFailReason.CallSelf);
			return;
		}
		if (TelephoneManager.GetCurrentActiveCalls() + 1 > TelephoneManager.MaxConcurrentCalls)
		{
			OnDialFailed(Telephone.DialFailReason.NetworkBusy);
			return;
		}
		PhoneController telephone = TelephoneManager.GetTelephone(number);
		if (telephone != null)
		{
			if (telephone.serverState == Telephone.CallState.Idle && telephone.CanReceiveCall())
			{
				SetPhoneState(Telephone.CallState.Dialing);
				lastDialedNumber = number;
				activeCallTo = telephone;
				activeCallTo.ReceiveCallFrom(this);
			}
			else
			{
				OnDialFailed(Telephone.DialFailReason.Engaged);
				telephone.OnIncomingCallWhileBusy();
			}
		}
		else
		{
			OnDialFailed(Telephone.DialFailReason.WrongNumber);
		}
	}

	private bool CanReceiveCall()
	{
		if (RequirePower && !IsPowered())
		{
			return false;
		}
		if (RequireParent && !base.baseEntity.HasParent())
		{
			return false;
		}
		return true;
	}

	public void AnswerPhone(BaseEntity.RPCMessage msg)
	{
		if (IsInvoking(TimeOutDialing))
		{
			CancelInvoke(TimeOutDialing);
		}
		if (!(activeCallTo == null))
		{
			BasePlayer player = msg.player;
			UpdateServerPlayer(player);
			BeginCall();
			activeCallTo.BeginCall();
		}
	}

	public void ReceiveCallFrom(PhoneController t)
	{
		activeCallTo = t;
		SetPhoneState(Telephone.CallState.Ringing);
		Invoke(TimeOutDialing, CallWaitingTime);
	}

	private void TimeOutDialing()
	{
		activeCallTo.ServerPlayAnsweringMessage(this);
		SetPhoneState(Telephone.CallState.Idle);
	}

	public void OnDialFailed(Telephone.DialFailReason reason)
	{
		SetPhoneState(Telephone.CallState.Idle);
		base.baseEntity.ClientRPC(null, "ClientOnDialFailed", (int)reason);
		activeCallTo = null;
		if (IsInvoking(TimeOutCall))
		{
			CancelInvoke(TimeOutCall);
		}
		if (IsInvoking(TriggerTimeOut))
		{
			CancelInvoke(TriggerTimeOut);
		}
		if (IsInvoking(TimeOutDialing))
		{
			CancelInvoke(TimeOutDialing);
		}
	}

	public void ServerPlayAnsweringMessage(PhoneController fromPhone)
	{
		OnDialFailed(Telephone.DialFailReason.TimedOut);
	}

	private void TriggerTimeOut()
	{
		OnDialFailed(Telephone.DialFailReason.TimedOut);
	}

	private void SetPhoneStateWithPlayer(Telephone.CallState state)
	{
		serverState = state;
		base.baseEntity.ClientRPC(null, "SetClientState", (int)serverState, (activeCallTo != null) ? activeCallTo.PhoneNumber : 0);
	}

	private void SetPhoneState(Telephone.CallState state)
	{
		if (state == Telephone.CallState.Idle && currentPlayer == null)
		{
			base.baseEntity.SetFlag(BaseEntity.Flags.Busy, false);
		}
		serverState = state;
		base.baseEntity.ClientRPC(null, "SetClientState", (int)serverState, (activeCallTo != null) ? activeCallTo.PhoneNumber : 0);
		Telephone telephone;
		if ((object)(telephone = (base.baseEntity as Telephone)) != null)
		{
			telephone.MarkDirtyForceUpdateOutputs();
		}
	}

	public void BeginCall()
	{
		SetPhoneStateWithPlayer(Telephone.CallState.InProcess);
		Invoke(TimeOutCall, TelephoneManager.MaxCallLength);
	}

	public void ServerHangUp(BaseEntity.RPCMessage msg)
	{
		if (!(msg.player != currentPlayer))
		{
			ServerHangUp();
		}
	}

	public void ServerHangUp()
	{
		if (activeCallTo != null)
		{
			activeCallTo.HangUp();
		}
		HangUp();
	}

	private void HangUp()
	{
		OnDialFailed(Telephone.DialFailReason.HangUp);
	}

	private void TimeOutCall()
	{
		OnDialFailed(Telephone.DialFailReason.TimeOutDuringCall);
	}

	public void OnReceivedVoiceFromUser(byte[] data)
	{
		if (activeCallTo != null)
		{
			activeCallTo.OnReceivedDataFromConnectedPhone(data);
		}
	}

	public void OnReceivedDataFromConnectedPhone(byte[] data)
	{
		base.baseEntity.ClientRPCEx(new SendInfo(BaseNetworkable.GetConnectionsWithin(base.baseEntity.GetNetworkPosition(), 15f))
		{
			priority = Priority.Immediate
		}, null, "OnReceivedVoice", data.Length, data);
	}

	public void OnIncomingCallWhileBusy()
	{
		base.baseEntity.ClientRPC(null, "OnIncomingCallDuringCall");
	}

	public void DestroyShared()
	{
		if (serverState != 0 && activeCallTo != null)
		{
			activeCallTo.HangUp();
		}
	}

	public void UpdatePhoneName(BaseEntity.RPCMessage msg)
	{
		if (!(msg.player != currentPlayer))
		{
			string text = msg.read.String();
			if (text.Length > 20)
			{
				text = text.Substring(0, 20);
			}
			if (Interface.CallHook("OnPhoneNameUpdate", this, text, msg.player) == null)
			{
				PhoneName = text;
				base.baseEntity.SendNetworkUpdate();
				Interface.CallHook("OnPhoneNameUpdated", this, PhoneName, msg.player);
			}
		}
	}

	public void Server_RequestPhoneDirectory(BaseEntity.RPCMessage msg)
	{
		if (!(msg.player != currentPlayer))
		{
			int page = msg.read.Int32();
			using (PhoneDirectory phoneDirectory = Pool.Get<PhoneDirectory>())
			{
				TelephoneManager.GetPhoneDirectory(PhoneNumber, page, 12, phoneDirectory);
				base.baseEntity.ClientRPC(null, "ReceivePhoneDirectory", phoneDirectory);
			}
		}
	}

	public void Server_AddSavedNumber(BaseEntity.RPCMessage msg)
	{
		if (!(msg.player != currentPlayer))
		{
			if (savedNumbers == null)
			{
				savedNumbers = Pool.Get<PhoneDirectory>();
			}
			if (savedNumbers.entries == null)
			{
				savedNumbers.entries = Pool.GetList<PhoneDirectory.DirectoryEntry>();
			}
			int num = msg.read.Int32();
			string text = msg.read.String();
			if (IsSavedContactValid(text, num) && savedNumbers.entries.Count < 10)
			{
				PhoneDirectory.DirectoryEntry directoryEntry = Pool.Get<PhoneDirectory.DirectoryEntry>();
				directoryEntry.phoneName = text;
				directoryEntry.phoneNumber = num;
				directoryEntry.ShouldPool = false;
				savedNumbers.ShouldPool = false;
				savedNumbers.entries.Add(directoryEntry);
				base.baseEntity.SendNetworkUpdate();
			}
		}
	}

	public void Server_RemoveSavedNumber(BaseEntity.RPCMessage msg)
	{
		if (!(msg.player != currentPlayer))
		{
			uint number = msg.read.UInt32();
			if (savedNumbers.entries.RemoveAll((PhoneDirectory.DirectoryEntry p) => p.phoneNumber == number) > 0)
			{
				base.baseEntity.SendNetworkUpdate();
			}
		}
	}

	public string GetDirectoryName()
	{
		return PhoneName;
	}

	private static string PositionToGridCoord(Vector3 position)
	{
		Vector2 a = new Vector2(TerrainMeta.NormalizeX(position.x), TerrainMeta.NormalizeZ(position.z));
		float num = TerrainMeta.Size.x / 1024f;
		int num2 = 7;
		Vector2 vector = a * num * num2;
		float num3 = Mathf.Floor(vector.x) + 1f;
		float num4 = Mathf.Floor(num * (float)num2 - vector.y);
		string str = string.Empty;
		float num5 = num3 / 26f;
		float num6 = num3 % 26f;
		if (num6 == 0f)
		{
			num6 = 26f;
		}
		if (num5 > 1f)
		{
			str += Convert.ToChar(64 + (int)num5).ToString();
		}
		str += Convert.ToChar(64 + (int)num6).ToString();
		return $"{str}{num4}";
	}

	private bool IsPowered()
	{
		IOEntity iOEntity;
		if (base.baseEntity != null && (object)(iOEntity = (base.baseEntity as IOEntity)) != null)
		{
			return iOEntity.IsPowered();
		}
		return false;
	}

	public bool IsSavedContactValid(string contactName, int contactNumber)
	{
		if (contactName.Length <= 0 || contactName.Length > 20)
		{
			return false;
		}
		if (contactNumber < 10000000 || contactNumber >= 100000000)
		{
			return false;
		}
		return true;
	}

	public void OnFlagsChanged(BaseEntity.Flags old, BaseEntity.Flags next)
	{
	}
}
