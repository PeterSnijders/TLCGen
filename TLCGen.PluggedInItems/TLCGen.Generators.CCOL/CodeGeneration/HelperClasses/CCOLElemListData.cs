﻿using System.Collections.Generic;

namespace TLCGen.Generators.CCOL.CodeGeneration
{
    public class CCOLElemListData
    {
        public List<CCOLElement> Elements { get; set; }

        public string CCOLCode { get; set; }
        public string CCOLSetting { get; set; }
        public string CCOLTType { get; set; }

        public int CCOLCodeWidth => CCOLCode?.Length ?? 0;
        public int CCOLSettingWidth => CCOLSetting?.Length ?? 0;
        public int CCOLTTypeWidth => CCOLTType?.Length ?? 0;

        public int TTypeMaxWidth { get; set; }
        public int DefineMaxWidth { get; set; }
        public int NameMaxWidth { get; set; }
        public int SettingMaxWidth { get; set; }
        public int CommentsMaxWidth { get; set; }

        public void SetMax()
        {
            foreach (var elem in this.Elements)
            {
                if (elem.Define?.Length > this.DefineMaxWidth) this.DefineMaxWidth = elem.Define.Length;
                if (elem.Naam?.Length > this.NameMaxWidth) this.NameMaxWidth = elem.Naam.Length;
                if (elem.Instelling?.ToString().Length > this.SettingMaxWidth) this.SettingMaxWidth = elem.Instelling.ToString().Length;
                if (elem.Commentaar?.Length > this.CommentsMaxWidth) this.CommentsMaxWidth = elem.Commentaar.Length;
                if (elem.TType.ToString().Length > this.TTypeMaxWidth) this.TTypeMaxWidth = elem.TType.ToString().Length;
            }
        }

        public CCOLElemListData()
        {
            DefineMaxWidth = 0;
            NameMaxWidth = 0;
            SettingMaxWidth = 0;

            Elements = new List<CCOLElement>();
        }
    }
}
