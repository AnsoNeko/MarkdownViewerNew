using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.SyntaxHighlighting;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace MarkdownViewerControl
{
    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design",
                      typeof(System.ComponentModel.Design.IDesigner))]
    public partial class MarkdownViewerNew : UserControl
    {

        static MarkdownViewerNew()
        {

        }

        private HtmlPanel _htmlPanel;
        private string _markdownText;
        private bool _isInitialized;
        private string _stylesheet = @"
                    body { 
                        font-family: Segoe UI, Arial; 
                        line-height: 1.6; 
                        padding: 8px;
                        color: {forecolor};
                        margin: 0;
                    }
                    .markdown-container {
                        max-width: 100%;
                        overflow: auto;
                    }
                    h1 { font-size: 2em; border-bottom: 1px solid #eee; padding-bottom: 0.3em; }
                    h2 { font-size: 1.5em; border-bottom: 1px solid #eee; padding-bottom: 0.3em; }
                    h3 { font-size: 1.25em; }
                    h4 { font-size: 1em; }
                    h5 { font-size: 0.875em; }
                    h6 { font-size: 0.85em; color: #777; }
                    .code-lang {
                        position: absolute;
                        right: 15px;
                        top: -10px;
                        background: #3498db;
                        color: white;
                        padding: 2px 8px;
                        border-radius: 3px;
                        font-size: 0.8em;
                        z-index: 1;
                    }
                    pre { 
                        position: relative; /* 添加定位上下文 */
                        margin-top: 20px; /* 为标签留出空间 */
                        background: #f6f8fa; 
                        padding: 16px; 
                        border-radius: 3px;
                        overflow: auto;
                        line-height: 1.45;
                        border-left: 3px solid #3498db;
                    }
                    code { 
                        background: rgba(27,31,35,0.05);
                        font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
                        padding: 0.2em 0.4em;
                        margin: 0;
                        font-size: 85%;
                        border-radius: 3px;
                    }
                    pre code {
                        background: transparent !important; /* 防止覆盖 */
                        padding: 0;
                        margin: 0;
                        border-radius: 0;
                        display: block; /* 新增 */
                        overflow-x: auto;
                    }

                    pre code.hljs {
                        display: block !important;
                        padding: 1em !important;
                        overflow-x: auto;
                        background: #f6f8fa !important;
                    }
                    pre code[class*=""language-""] {
                        background: #f6f8fa !important;
                        padding: 1em !important;
                        border-radius: 4px;
                        display: block !important;
                    }
                    div[class^=""lang-""] {
                        display: none !important;
                        padding: 1em !important;
                        background: #f6f8fa !important;
                        border-radius: 4px;
                        margin: 8px 0;
                        overflow-x: auto;
                    }

                    blockquote { 
                        border-left: 4px solid #dfe2e5; 
                        padding: 0 1em;
                        color: #6a737d;
                        margin-left: 0;
                    }
                    table { 
                        border-collapse: collapse; 
                        width: 100%; 
                        margin: 16px 0;
                        overflow: auto;
                    }
                    th, td { 
                        border: 1px solid #dfe2e5; 
                        padding: 6px 13px; 
                    }
                    th { 
                        background-color: #f6f8fa; 
                        font-weight: 600;
                    }
                    tr {
                        background-color: #fff;
                        border-top: 1px solid #c6cbd1;
                    }
                    tr:nth-child(2n) {
                        background-color: #f6f8fa;
                    }
                    img {
                        max-width: 100%;
                        box-sizing: content-box;
                        background-color: #fff;
                    }
                    .mermaid {
                        background-color: white;
                        padding: 16px;
                        border-radius: 3px;
                        margin: 16px 0;
                        text-align: center;
                        overflow: auto;  /* 新增滚动条 */
                        min-height: 100px; /* 防止折叠 */
                        background: #fff !important;
                        border: 1px solid #eee;
                        font-family: Arial !important;
                    }
                    .mermaid svg {
                        max-width: 100%;
                        height: auto !important;
                    }

                    .mermaid[data-processed] {
                        background: #fff !important;
                        padding: 20px;
                        border-radius: 8px;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    }";

        private MarkdownPipeline _pipeline;
        private bool _enableMermaid = false;
        private bool _enableSyntaxHighlight = true;
        private bool _enableAutoIdentifiers = true;

        [Category("Appearance")]
        [Description("The Markdown text to display")]
        public string MarkdownText
        {
            get => _markdownText;
            set
            {
                _markdownText = value;
                if (_isInitialized) SafeRenderMarkdown();
            }
        }

        [Category("Behavior")]
        [Description("Enable Mermaid diagram support")]
        [DefaultValue(false)]
        public bool EnableMermaid
        {
            get => _enableMermaid;
            set
            {
                _enableMermaid = value;
                InitializePipeline();
                SafeRenderMarkdown();
            }
        }

        [Category("Behavior")]
        [Description("Enable syntax highlighting for code blocks")]
        [DefaultValue(true)]
        public bool EnableSyntaxHighlight
        {
            get => _enableSyntaxHighlight;
            set
            {
                _enableSyntaxHighlight = value;
                InitializePipeline();
                SafeRenderMarkdown();
            }
        }

        [Category("Behavior")]
        [Description("Enable automatic header identifiers")]
        [DefaultValue(true)]
        public bool EnableAutoIdentifiers
        {
            get => _enableAutoIdentifiers;
            set
            {
                _enableAutoIdentifiers = value;
                InitializePipeline();
                SafeRenderMarkdown();
            }
        }

        private string _customCss = "";

        [Category("Appearance")]
        [Description("Additional CSS styles")]
        public string CustomCss
        {
            get => _customCss;
            set
            {
                _customCss = value;
                SafeRenderMarkdown();
            }
        }

        public MarkdownViewerNew()
        {
            this.SetStyle(ControlStyles.ResizeRedraw |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor,
                 true);

            InitializeDesignerComponent();
            this.BackColor = Color.White;
            this.Padding = new Padding(8);
            this.AutoScroll = true;
            InitializePipeline();

            this.HandleCreated += (s, e) =>
            {
                Debug.WriteLine($"HandleCreated, Parent: {this.Parent?.Name ?? "null"}");
                _isInitialized = true;
                SafeRenderMarkdown();
            };



            this.VisibleChanged += (s, e) =>
            {
                Debug.WriteLine($"VisibleChanged: {this.Visible}");
                if (this.Visible) SafeRenderMarkdown();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (DesignMode)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen,
                        ClientRectangle.X,
                        ClientRectangle.Y,
                        ClientRectangle.Width - 1,
                        ClientRectangle.Height - 1);
                }
            }
        }

        private void InitializePipeline()
        {
            var pipelineBuilder = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()  // 必须添加
                .UsePipeTables()         // 启用表格支持
                .UseGridTables()         // 启用表格支持
                .UseGenericAttributes(); // 允许HTML属性
            pipelineBuilder.Extensions.Add(new SafeSyntaxHighlightingExtension());


            // 安全启用语法高亮
            if (_enableSyntaxHighlight)
            {
                pipelineBuilder = pipelineBuilder.UseSyntaxHighlighting();
            }

            if (_enableAutoIdentifiers)
            {
                pipelineBuilder = pipelineBuilder.UseAutoIdentifiers(AutoIdentifierOptions.GitHub);
            }

            _pipeline = pipelineBuilder.Build();

            // 新版Markdig需要单独配置渲染器
            if (_enableSyntaxHighlight && _pipeline.Extensions.Find<SyntaxHighlightingExtension>() != null)
            {
                var renderer = new HtmlRenderer(new StringWriter());
                _pipeline.Setup(renderer);
            }
        }

        // 创建安全渲染器（兼容新版Markdig）
        private class SafeSyntaxHighlightingExtension : IMarkdownExtension
        {
            public void Setup(MarkdownPipelineBuilder pipeline) { }

            public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
            {
                if (renderer is HtmlRenderer htmlRenderer)
                {
                    // 替换默认的代码块渲染器
                    var original = htmlRenderer.ObjectRenderers.Find<CodeBlockRenderer>();
                    if (original != null)
                    {
                        htmlRenderer.ObjectRenderers.Remove(original);
                        htmlRenderer.ObjectRenderers.Add(new SafeCodeBlockRenderer());
                    }
                }
            }
        }

        // 安全代码块渲染器实现
        private class SafeCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
        {
            protected override void Write(HtmlRenderer renderer, CodeBlock obj)
            {
                try
                {
                    if (obj is FencedCodeBlock fenced)
                    {
                        var language = fenced.Info?.Replace("language-", "");
                        renderer.Write($"<pre><code class=\"language-{language}\">");

                        // 关键修复：添加完整参数
                        renderer.WriteLeafRawLines(
                            leafBlock: fenced,          // LeafBlock 实例
                            writeEndOfLines: true,      // 写入换行符
                            escape: false          // 不转义 HTML
                        );

                        renderer.Write("</code></pre>");
                    }
                    else
                    {
                        renderer.Write("<pre><code>");

                        // 处理普通代码块（需转换为 LeafBlock）
                        if (obj is LeafBlock leaf)
                        {
                            renderer.WriteLeafRawLines(
                                leafBlock: leaf,
                                writeEndOfLines: true,
                                escape: false
                            );
                        }
                        else
                        {
                            // 兼容旧版处理方式
                            renderer.Write("<pre><code>");
                            renderer.Write(obj.Lines.ToString());
                            renderer.Write("</code></pre>");
                        }

                        renderer.Write("</code></pre>");
                    }
                }
                catch
                {
                    renderer.Write("<pre><code>ERROR RENDERING CODE BLOCK</code></pre>");
                }
            }
        }



        private void InitializeDesignerComponent()
        {
            InitializeComponent();
            _htmlPanel = new HtmlPanel
            {
                Name = "htmlPanel",
                Dock = DockStyle.Fill,
                BackColor = this.BackColor,
                ForeColor = this.ForeColor,
                BaseStylesheet = _stylesheet.Replace("{forecolor}", ColorTranslator.ToHtml(this.ForeColor)),
                AutoScroll = true,
                BorderStyle = BorderStyle.None
            };

            _htmlPanel.Paint += (s, e) =>
            {
                if (DesignMode)
                {
                    using (var pen = new Pen(Color.Blue, 1))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, _htmlPanel.Width - 1, _htmlPanel.Height - 1);
                    }
                }
            };

            this.Controls.Add(_htmlPanel);

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                _markdownText = "# 1234 \r\n## 2234";
                //_htmlPanel.Text = "<div style='padding:10px;color:blue'><b>设计模式激活</b></div>";
            }

        }

        public void ForceRender()
        {
            if (this.IsHandleCreated)
            {
                Debug.WriteLine("ForceRender invoked");
                this.BeginInvoke(new Action(() =>
                {
                    SafeRenderMarkdown();
                    this.Invalidate(true);
                }));
            }
        }

        private void SafeRenderMarkdown()
        {
            if (!this.IsHandleCreated || !_isInitialized)
            {
                Debug.WriteLine($"SafeRender skipped - Handle: {this.IsHandleCreated}, Init: {_isInitialized}");
                return;
            }

            if (this.InvokeRequired)
            {
                Debug.WriteLine("Cross-thread render detected");
                this.Invoke(new Action(RenderMarkdown));
            }
            else
            {
                RenderMarkdown();
            }
        }

        private void RenderMarkdown()
        {
            if (!_isInitialized || _htmlPanel == null) return;

            _htmlPanel.SuspendLayout();
            try
            {
                string html;
                try
                {
                    html = string.IsNullOrEmpty(_markdownText)
                        ? "<div class='empty'>```mermaid\r\n\r\ngraph TD;\r\n    A[开始] --> B{条件判断};\r\n    B -->|是| C[执行操作1];\r\n    B -->|否| D[执行操作2];\r\n    C --> E[结束];\r\n    D --> E[结束];\r\n\r\n```</div>"
                        : Markdig.Markdown.ToHtml(_markdownText, _pipeline);
                    var debugHtml = BuildFinalHtml(html);
                    Debug.WriteLine("=== 生成 HTML ===");
                    Debug.WriteLine(debugHtml);
                    Debug.WriteLine("================");
                    _htmlPanel.Text = debugHtml;
                }
                catch (NullReferenceException)
                {
                    // 语法高亮失败时回退
                    var fallbackPipeline = new MarkdownPipelineBuilder()
                        .UseAdvancedExtensions()
                        .UsePipeTables()
                        .UseGridTables()
                        .Build();
                    html = Markdig.Markdown.ToHtml(_markdownText, fallbackPipeline);
                }

                if (_enableMermaid)
                {
                    html = ProcessMermaidBlocks(html);
                }

                _htmlPanel.Text = BuildFinalHtml(html);
            }
            catch (Exception ex)
            {
                _htmlPanel.Text = $"<div style='color:red'>渲染错误: {ex.Message}</div>";
                Debug.WriteLine($"Render failed: {ex}");
            }
            finally
            {
                _htmlPanel.ResumeLayout();
            }
        }

        private string BuildFinalHtml(string bodyHtml)
        {
            var resourcePath = Path.Combine(Application.StartupPath, "Resources");
            return $@"<!DOCTYPE html>
<html>
<head>
    <link rel=""stylesheet"" href=""file:///{resourcePath}/highlight/styles/github.min.css"">
    <script src=""file:///{resourcePath}/highlight/highlight.min.js""></script>
    <script src=""file:///{resourcePath}/mermaid/mermaid.min.js""></script>
    <style>
        {_stylesheet.Replace("{forecolor}", ColorTranslator.ToHtml(this.ForeColor))}
        {_customCss}
    </style>
    <script>
        // 修改后的初始化脚本
    document.addEventListener('DOMContentLoaded', () => {{
        // 先初始化Mermaid
        mermaid.initialize({{
            startOnLoad: false,
            securityLevel: 'loose',
            theme: 'neutral',
            flowchart: {{ curve: 'linear' }}
        }});
        
        // 手动渲染所有Mermaid图表
        mermaid.init(undefined, '.mermaid');

        // 高亮处理排除.mermaid类
        document.querySelectorAll('pre code[class^=""language-""]:not(.mermaid)').forEach((block) => {{
            hljs.highlightElement(block);
            // 添加语言标签
            const lang = block.className.replace('language-', '');
            if(lang) {{
                block.parentNode.insertAdjacentHTML('afterbegin', 
                    `<div class=""code-lang"">${{lang.toUpperCase()}}</div>`);
            }}
        }});

        // 强制重绘
        setTimeout(() => {{
            mermaid.init();
        }}, 100);
    }});
    </script>
</head>
<body>{bodyHtml}</body>
</html>";
        }
 


        // 新增独立处理方法
        private string ProcessMermaidBlocks(string html)
        {
            try
            {
                if (!_enableMermaid) return html;

                return Regex.Replace(html,
            @"<pre><code class=""language-mermaid"">([\s\S]*?)<\/code><\/pre>",
            match =>
            {
                var content = WebUtility.HtmlDecode(match.Groups[1].Value)
                    .Replace("\\\"", "\"")  // 处理转义字符
                    .Replace("&amp;", "&")
                    .Replace("&lt;", "<")
                    .Replace("&gt;", ">")
                    .Replace("&#39;", "'")
                    .Replace("&#xD;&#xA;", "\n");

                return $@"
                <div class='mermaid-container'>
                    <div class='mermaid'>{content.Trim()}</div>
                    <div class='mermaid-source' style='display:none'>
                        <pre>{content.Trim()}</pre>
                    </div>
                </div>";
            },
            RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Mermaid processing failed: {ex}");
                return html + $"<!-- Mermaid Error: {ex.Message} -->";
            }
        }


        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            Debug.WriteLine($"Layout - Size: {Size}, Client: {ClientSize}");
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            if (_htmlPanel != null)
            {
                _htmlPanel.BackColor = this.BackColor;
                SafeRenderMarkdown();
            }
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            if (_htmlPanel != null)
            {
                _htmlPanel.BaseStylesheet = _stylesheet.Replace("{forecolor}", ColorTranslator.ToHtml(this.ForeColor));
                SafeRenderMarkdown();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Debug.WriteLine($"Resize: {Size}");
            _htmlPanel?.Invalidate();
        }


        public void LoadMarkdownFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                MarkdownText = File.ReadAllText(filePath);
            }
        }
    }
}