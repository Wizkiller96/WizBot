﻿#nullable disable
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace NadekoBot.Common.Yml;

public class CommentGatheringTypeInspector : TypeInspectorSkeleton
{
    private readonly ITypeInspector innerTypeDescriptor;

    public CommentGatheringTypeInspector(ITypeInspector innerTypeDescriptor)
        => this.innerTypeDescriptor = innerTypeDescriptor ?? throw new ArgumentNullException("innerTypeDescriptor");

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        => innerTypeDescriptor.GetProperties(type, container).Select(d => new CommentsPropertyDescriptor(d));

    private sealed class CommentsPropertyDescriptor : IPropertyDescriptor
    {
        public string Name { get; }

        public Type Type
            => baseDescriptor.Type;

        public Type TypeOverride
        {
            get => baseDescriptor.TypeOverride;
            set => baseDescriptor.TypeOverride = value;
        }

        public int Order { get; set; }

        public ScalarStyle ScalarStyle
        {
            get => baseDescriptor.ScalarStyle;
            set => baseDescriptor.ScalarStyle = value;
        }

        public bool CanWrite
            => baseDescriptor.CanWrite;

        private readonly IPropertyDescriptor baseDescriptor;

        public CommentsPropertyDescriptor(IPropertyDescriptor baseDescriptor)
        {
            this.baseDescriptor = baseDescriptor;
            Name = baseDescriptor.Name;
        }

        public void Write(object target, object value)
            => baseDescriptor.Write(target, value);

        public T GetCustomAttribute<T>()
            where T : Attribute
            => baseDescriptor.GetCustomAttribute<T>();

        public IObjectDescriptor Read(object target)
        {
            var comment = baseDescriptor.GetCustomAttribute<CommentAttribute>();
            return comment is not null
                ? new CommentsObjectDescriptor(baseDescriptor.Read(target), comment.Comment)
                : baseDescriptor.Read(target);
        }
    }
}