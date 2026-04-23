using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

namespace Liminal.Core.Fader
{
	public class ImageScreenFader : ScreenFaderBase
    {
		[SerializeField] private Image m_Image;

		#region Properties

		/// <summary>
		/// Gets or sets the image the fade is applied to.
		/// </summary>
		public Image Image
		{
			get { return m_Image; }
			set { m_Image = value; }
		}

		#endregion
		
		protected override void OnAwake()
		{
			if (m_Image == null)
				m_Image = GetComponentInChildren<Image>();
		}
		
		protected override void ApplyColor(Color color)
		{
			if (m_Image != null)
				m_Image.color = color;
		}
	}
}