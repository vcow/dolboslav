using Base.Activatable;
using DG.Tweening;
using UnityEngine;

namespace Windows
{
	/// <summary>
	/// Point view controller for the TableWindow records. Also uses in the TableWindowPointSelector
	/// that's why implements IActivatable (see https://github.com/vcow/lib-logicality for details).
	/// </summary>
	public sealed class TableWindowPointViewController : TablePointViewController, IActivatable
	{
		private ActivatableState _activatableState = ActivatableState.Active;

		private const float ActivateDuration = 0.5f;
		private const float DeactivateDuration = 0.3f;

		[SerializeField] private int _idolNumber;

		public int IdolNumber => _idolNumber;

		public override void Pulse(bool play = true)
		{
			if (!play || this.IsActive())
			{
				base.Pulse(play);
			}
		}

		public void Activate(bool immediately = false)
		{
			DOTween.Kill(Icon);
			if (immediately)
			{
				Icon.color = UnityEngine.Color.white;
				ActivatableState = ActivatableState.Active;
			}
			else
			{
				ActivatableState = ActivatableState.ToActive;
				Icon.color = new Color(1f, 1f, 1f, 0f);
				Icon.DOFade(1, ActivateDuration).SetEase(Ease.InQuad).SetLink(Icon.gameObject)
					.OnComplete(() => ActivatableState = ActivatableState.Active);
			}
		}

		public void Deactivate(bool immediately = false)
		{
			DOTween.Kill(Icon);
			if (immediately)
			{
				Icon.color = new Color(1f, 1f, 1f, 0f);
				ActivatableState = ActivatableState.Inactive;
			}
			else
			{
				ActivatableState = ActivatableState.ToInactive;
				Icon.color = UnityEngine.Color.white;
				Icon.DOFade(0, DeactivateDuration).SetEase(Ease.InQuad).SetLink(Icon.gameObject)
					.OnComplete(() => ActivatableState = ActivatableState.Inactive);
			}
		}

		public ActivatableState ActivatableState
		{
			get => _activatableState;
			private set
			{
				if (value == _activatableState)
				{
					return;
				}

				_activatableState = value;
				ActivatableStateChangedEvent?.Invoke(this, _activatableState);
			}
		}

		public event ActivatableStateChangedHandler ActivatableStateChangedEvent;
	}
}