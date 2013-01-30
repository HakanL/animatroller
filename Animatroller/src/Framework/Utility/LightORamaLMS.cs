using System.Xml.Serialization;

namespace Animatroller.Framework.Import.Schemas.LightORama.LMS
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class channels
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("channel", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public channelsChannel[] channel;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class channelsChannel
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("effect", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public channelsChannelEffect[] effect;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string color;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public long centiseconds;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string deviceType;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int unit;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int circuit;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int savedIndex;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class channelsChannelEffect
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public long startCentisecond;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public long endCentisecond;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public short intensity;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string startIntensity;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string endIntensity;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class sequence
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("channel", typeof(channelsChannel), Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = false)]
        public channelsChannel[] channels;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        [System.Xml.Serialization.XmlArrayItemAttribute("timingGrid", typeof(sequenceTimingGridsTimingGrid), Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = false)]
        public sequenceTimingGridsTimingGrid[] timingGrids;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        [System.Xml.Serialization.XmlArrayItemAttribute("track", typeof(sequenceTracksTrack), Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = false)]
        public sequenceTracksTrack[] tracks;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("animation", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public sequenceAnimation[] animation;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string saveFileVersion;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string createdAt;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string modifiedBy;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string musicAlbum;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string musicArtist;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string musicFilename;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string musicTitle;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class sequenceTimingGridsTimingGrid
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("timing", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public sequenceTimingGridsTimingGridTiming[] timing;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string saveID;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class sequenceTimingGridsTimingGridTiming
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public long centisecond;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class sequenceTracksTrack
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string loopLevels;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("channel", typeof(channelsChannel), Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = false)]
        public channelsChannel[] channels;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public long totalCentiseconds;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string timingGrid;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class sequenceAnimation
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("row", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public sequenceAnimationRow[] row;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string rows;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string columns;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string image;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string hideControls;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class sequenceAnimationRow
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("column", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public sequenceAnimationRowColumn[] column;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string index;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class sequenceAnimationRowColumn
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string index;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string channel;
    }
}
