import { defineConfig } from 'vitepress'

const repo = 'cities-skylines1-agent-skill'
const base = `/${repo}/`

export default defineConfig({
  title: 'Cities: Skylines 1 Agent Skill',
  description: 'Codex skill and CS1 mod for API-driven city inspection, repair, and saving.',
  base,
  cleanUrls: true,
  head: [
    ['link', { rel: 'icon', type: 'image/svg+xml', href: `${base}agent-bridge-icon.svg` }],
    ['meta', { property: 'og:title', content: 'Cities: Skylines 1 Agent Skill' }],
    ['meta', { property: 'og:description', content: 'Operate Cities: Skylines 1 through a local API bridge built for AI agents.' }]
  ],
  themeConfig: {
    logo: '/agent-bridge-icon.svg',
    siteTitle: 'CS1 Agent Skill',
    nav: [
      { text: 'Guide', link: '/guide/getting-started' },
      { text: 'API', link: '/api' },
      { text: 'Releases', link: '/releases/v0.3.0/' },
      { text: 'Development', link: '/guide/development-flow' },
      { text: 'Japanese', link: '/ja/' },
      { text: 'GitHub', link: 'https://github.com/Sunwood-ai-labs/cities-skylines1-agent-skill' }
    ],
    sidebar: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/guide/getting-started' },
          { text: 'Agent Workflow', link: '/guide/usage' },
          { text: 'Development Flow', link: '/guide/development-flow' },
          { text: 'Architecture', link: '/guide/architecture' },
          { text: 'Troubleshooting', link: '/guide/troubleshooting' }
        ]
      },
      {
        text: 'Reference',
        items: [
          { text: 'API Reference', link: '/api' },
          { text: 'Release Notes v0.3.0', link: '/releases/v0.3.0/' },
          { text: 'Release Notes v0.1.0', link: '/releases/v0.1.0/' },
          { text: 'v0.3.0 Walkthrough', link: '/guide/articles/v0-3-0-agent-city-operations' },
          { text: 'Experiment Article (JA)', link: '/articles/building-cities-skylines-with-ai-agents-ja' }
        ]
      }
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/Sunwood-ai-labs/cities-skylines1-agent-skill' }
    ],
    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Cities: Skylines is a trademark of Colossal Order / Paradox Interactive. This project is an independent experimental bridge.'
    }
  },
  locales: {
    root: {
      label: 'English',
      lang: 'en-US'
    },
    ja: {
      label: '日本語',
      lang: 'ja-JP',
      title: 'Cities: Skylines 1 Agent Skill',
      description: 'AIエージェントがCS1をAPI経由で調査・修復・保存するためのCodex SkillとMOD。',
      themeConfig: {
        nav: [
          { text: 'ガイド', link: '/ja/guide/getting-started' },
          { text: 'API', link: '/ja/api' },
          { text: 'リリース', link: '/ja/releases/v0.3.0/' },
          { text: '開発フロー', link: '/ja/guide/development-flow' },
          { text: 'English', link: '/' },
          { text: 'GitHub', link: 'https://github.com/Sunwood-ai-labs/cities-skylines1-agent-skill' }
        ],
        sidebar: [
          {
            text: 'ガイド',
            items: [
              { text: 'はじめに', link: '/ja/guide/getting-started' },
              { text: 'エージェント運用', link: '/ja/guide/usage' },
              { text: '開発フロー', link: '/ja/guide/development-flow' },
              { text: '構成', link: '/ja/guide/architecture' },
              { text: 'トラブルシュート', link: '/ja/guide/troubleshooting' }
            ]
          },
          {
            text: 'リファレンス',
            items: [
              { text: 'APIリファレンス', link: '/ja/api' },
              { text: 'リリースノート v0.3.0', link: '/ja/releases/v0.3.0/' },
              { text: 'リリースノート v0.1.0', link: '/ja/releases/v0.1.0/' },
              { text: 'v0.3.0 リリース内容', link: '/ja/guide/articles/cs1-agent-skill-beginner' },
              { text: 'v0.3.0 解説記事', link: '/ja/guide/articles/v0-3-0-agent-city-operations' },
              { text: '実験記事', link: '/articles/building-cities-skylines-with-ai-agents-ja' }
            ]
          }
        ]
      }
    }
  }
})
