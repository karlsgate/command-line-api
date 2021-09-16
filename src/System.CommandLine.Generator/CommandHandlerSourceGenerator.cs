﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace System.CommandLine.Generator
{
    [Generator]
    public class CommandHandlerSourceGenerator : ISourceGenerator
    {
        private const string ICommandHandlerType = "System.CommandLine.Invocation.ICommandHandler";

        public void Execute(GeneratorExecutionContext context)
        {
            SyntaxReceiver rx = (SyntaxReceiver)context.SyntaxContextReceiver!;

            StringBuilder builder = new();
            builder.Append(
$@"// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Binding;
using System.Reflection;
using System.Threading.Tasks;
using System.CommandLine.Invocation;

namespace {typeof(CommandHandlerGeneratorExtensions).Namespace}
{{
    public static partial class CommandHandlerGeneratorExtensions_Generated
    {{
");
            int handlerCount = 1;

            foreach (var invocation in rx.Invocations)
            {
                var methodParameters = invocation.Parameters
                                                 .Select(x => x.GetMethodParameter())
                                                 .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                                                 .ToArray();

                builder.Append(
                    @$"
        public static {ICommandHandlerType} {nameof(CommandHandlerGeneratorExtensions.Create)}<{string.Join(", ", Enumerable.Range(1, invocation.NumberOfGenerericParameters).Select(x => $@"T{x}"))}>(
            this {nameof(CommandHandlerGenerator)} handler,");
                builder.Append($@"
            {invocation.DelegateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} method");

                if (methodParameters.Length > 0)
                {
                    builder.Append(",");
                    builder.AppendLine(string.Join(", ", methodParameters.Select(x => $@"
            {x.Type} {x.Name}")) + ")");
                }
                else
                {
                    builder.Append(")");
                }

                builder.Append(@"
        {");
                builder.Append($@"
            return new GeneratedHandler_{handlerCount}(method");

                if (methodParameters.Length > 0)
                {
                    builder.Append(", ");
                    builder.Append(string.Join(", ", methodParameters.Select(x => x.Name)));
                }

                builder.Append(");");

                builder.AppendLine(@"
        }");

                //TODO: fully qualify type names
                builder.Append($@"
        private class GeneratedHandler_{handlerCount} : {ICommandHandlerType}
        {{
            public GeneratedHandler_{handlerCount}(
                {invocation.DelegateType} method");

                if (methodParameters.Length > 0)
                {
                    builder.Append(",");
                    builder.Append(string.Join($", ", methodParameters.Select(x => $@"
                {x.Type} {x.Name}")) + ")");
                }
                else
                {
                    builder.Append(")");
                }

                builder.Append($@"
            {{
                Method = method;");
                foreach (var propertyAssignment in invocation.Parameters
                                                             .Select(x => x.GetPropertyAssignment())
                                                             .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    builder.Append($@"
                {propertyAssignment}");
                }

                builder.AppendLine($@"
            }}
                
            public {invocation.DelegateType} Method {{ get; }}");

                foreach (var propertyDeclaration in invocation.Parameters
                                                              .Select(x => x.GetPropertyDeclaration())
                                                              .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    builder.Append($@"
            {propertyDeclaration}");
                }

                builder.Append($@"
            public async Task<int> InvokeAsync(InvocationContext context)
            {{");
                builder.Append($@"
                {invocation.InvokeContents()}");
                builder.Append($@"
            }}
        }}");
                handlerCount++;
            }

            builder.Append(@"
    }
}
");

            context.AddSource("CommandHandlerGeneratorExtensions_Generated.g.cs", builder.ToString());
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }
    }
}