<script setup lang="ts">
import { useAuth } from '~/composables/useAuth'

const route = useRoute()
const { isAuthenticated, isLoading, initializeAuth } = useAuth()

onMounted(() => {
  initializeAuth()
})

// Feature data mapping
const featuresData: Record<string, {
  title: string
  subtitle: string
  description: string
  icon: string
  color: string
  heroImage?: string
  benefits: { title: string; description: string; icon: string }[]
  capabilities: { title: string; items: string[] }[]
  useCases: { title: string; description: string; industry: string }[]
  testimonial?: { quote: string; author: string; role: string; company: string }
}> = {
  'business-model-canvas': {
    title: 'Business Model Canvas',
    subtitle: 'Strategic Planning with Connected Canvases',
    description: 'Visualize and manage your entire business model with interconnected canvases. Link strategic elements directly to operational data for real-time insights and AI-powered recommendations.',
    icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2',
    color: 'purple',
    benefits: [
      {
        title: 'Visual Strategy',
        description: 'See your entire business model at a glance with the classic 9-block canvas layout.',
        icon: 'M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5z'
      },
      {
        title: 'Connected Data',
        description: 'Link canvas blocks to real operational entities - roles, processes, partners, and more.',
        icon: 'M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101'
      },
      {
        title: 'Multiple Scopes',
        description: 'Create canvases at organization, product, or segment level for granular strategic planning.',
        icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2'
      },
      {
        title: 'AI Insights',
        description: 'Get AI-generated summaries and recommendations based on your canvas content.',
        icon: 'M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z'
      }
    ],
    capabilities: [
      {
        title: 'Canvas Blocks',
        items: ['Key Partners', 'Key Activities', 'Key Resources', 'Value Propositions', 'Customer Relationships', 'Channels', 'Customer Segments', 'Cost Structure', 'Revenue Streams']
      },
      {
        title: 'View Modes',
        items: ['Classic Canvas View', 'Kanban Board', 'List View', 'AI Summary View']
      },
      {
        title: 'Entity Linking',
        items: ['Link Roles to Key Resources', 'Link Processes to Key Activities', 'Link Partners directly', 'Drill-down to operational details']
      }
    ],
    useCases: [
      {
        title: 'Startup Strategy',
        description: 'Map your business model hypotheses and track validation as you build.',
        industry: 'Startups'
      },
      {
        title: 'Product Portfolio',
        description: 'Manage multiple product canvases with shared resources and synergies.',
        industry: 'Product Companies'
      },
      {
        title: 'Strategic Planning',
        description: 'Align executive teams around a single source of strategic truth.',
        industry: 'Enterprise'
      }
    ]
  },
  'organizational-intelligence': {
    title: 'Organizational Intelligence',
    subtitle: 'Org Charts, Roles & Capacity Planning',
    description: 'Map your organizational structure, define roles and functions, and let AI identify coverage gaps, single points of failure, and capacity issues before they become problems.',
    icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z',
    color: 'blue',
    benefits: [
      {
        title: 'Visual Org Chart',
        description: 'Interactive org chart with multiple view modes - tree, cards, and list.',
        icon: 'M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z'
      },
      {
        title: 'Role Management',
        description: 'Define roles with functions, assign people, and track allocation percentages.',
        icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z'
      },
      {
        title: 'Coverage Analysis',
        description: 'AI identifies gaps, single points of failure, and understaffed functions.',
        icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z'
      },
      {
        title: 'Capacity Tracking',
        description: 'Monitor workload distribution and identify overloaded team members.',
        icon: 'M13 7h8m0 0v8m0-8l-8 8-4-4-6 6'
      }
    ],
    capabilities: [
      {
        title: 'People Management',
        items: ['Team member profiles', 'Role assignments', 'Allocation tracking', 'Status monitoring', 'Reporting relationships']
      },
      {
        title: 'Role Configuration',
        items: ['Department categorization', 'Function assignment', 'Coverage status', 'Vacancy management']
      },
      {
        title: 'Org Chart Views',
        items: ['Tree view (Vue Flow)', 'Card grid view', 'List/table view', 'Metrics dashboard']
      }
    ],
    useCases: [
      {
        title: 'Growth Planning',
        description: 'Plan hiring by identifying coverage gaps before they impact operations.',
        industry: 'HR & Operations'
      },
      {
        title: 'M&A Integration',
        description: 'Map both organizations to identify overlaps and integration opportunities.',
        industry: 'Private Equity'
      },
      {
        title: 'Compliance',
        description: 'Ensure proper segregation of duties and role-based access control.',
        industry: 'Regulated Industries'
      }
    ]
  },
  'process-automation': {
    title: 'Process Automation',
    subtitle: 'Visual Process Design & Optimization',
    description: 'Design, document, and optimize business processes with intuitive visual flow editors. Connect processes to the people who execute them and get AI-powered recommendations for improvement.',
    icon: 'M13 10V3L4 14h7v7l9-11h-7z',
    color: 'emerald',
    benefits: [
      {
        title: 'Visual Editor',
        description: 'Drag-and-drop process flow designer powered by Vue Flow.',
        icon: 'M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5z'
      },
      {
        title: 'Activity Types',
        description: 'Model manual, automated, hybrid, decision, and handoff activities.',
        icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2'
      },
      {
        title: 'Resource Linking',
        description: 'Connect activities to roles, functions, and systems that execute them.',
        icon: 'M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101'
      },
      {
        title: 'Process States',
        description: 'Track current vs. target state processes for transformation planning.',
        icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5'
      }
    ],
    capabilities: [
      {
        title: 'Flow Editor',
        items: ['Start/End nodes', 'Activity nodes', 'Decision gates', 'Handoff points', 'Custom connections']
      },
      {
        title: 'Activity Properties',
        items: ['Name & description', 'Activity type', 'Responsible function', 'Systems involved', 'Duration estimates']
      },
      {
        title: 'Process Metadata',
        items: ['Purpose statement', 'Trigger conditions', 'Expected outputs', 'Status tracking', 'Version history']
      }
    ],
    useCases: [
      {
        title: 'Operations Mapping',
        description: 'Document core business processes for training and compliance.',
        industry: 'Operations'
      },
      {
        title: 'Transformation',
        description: 'Design future-state processes for digital transformation initiatives.',
        industry: 'Consulting'
      },
      {
        title: 'Automation Planning',
        description: 'Identify automation opportunities by analyzing activity types.',
        industry: 'Technology'
      }
    ]
  },
  'ai-agents': {
    title: 'Multi-Agent AI Chat',
    subtitle: 'Specialized AI Agents for Your Business',
    description: 'Collaborate with AI agents that understand your business context. Create specialized agents for different domains - CFO, Operations, Strategy, HR - and have them work together on complex problems. Each agent can be fully configured with unique personalities, expertise areas, and behavioral traits.',
    icon: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z',
    color: 'cyan',
    benefits: [
      {
        title: 'Custom Agents',
        description: 'Create AI agents with specialized system prompts, personas, and configurable personality traits.',
        icon: 'M5.121 17.804A13.937 13.937 0 0112 16c2.5 0 4.847.655 6.879 1.804M15 10a3 3 0 11-6 0 3 3 0 016 0zm6 2a9 9 0 11-18 0 9 9 0 0118 0z'
      },
      {
        title: 'Context-Aware',
        description: 'Agents have full read access to your organization data including roles, processes, canvases, and more.',
        icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2'
      },
      {
        title: 'Multi-Agent Chat',
        description: 'Bring multiple agents into a conversation for diverse perspectives and collaborative problem-solving.',
        icon: 'M17 8h2a2 2 0 012 2v6a2 2 0 01-2 2h-2v4l-4-4H9a1.994 1.994 0 01-1.414-.586m0 0L11 14h4a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2v4l.586-.586z'
      },
      {
        title: 'Action Proposals',
        description: 'AI agents can propose changes to your data through a human-in-the-loop approval workflow.',
        icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z'
      }
    ],
    capabilities: [
      {
        title: 'Agent Configuration',
        items: ['System prompt templates', 'Model selection (Claude Sonnet/Opus/Haiku)', 'Temperature & token limits', 'Avatar color customization', 'Active/inactive toggle']
      },
      {
        title: 'Conversation Modes',
        items: ['On-Demand (@mention invocation)', 'Round-Robin (agents take turns)', 'Moderated (human approval)', 'Free-form (agents interact freely)']
      },
      {
        title: 'Data Access & Actions',
        items: ['Full organization context', 'Propose CRUD operations', 'Pending action workflow', 'Complete audit trail', 'Token usage tracking']
      }
    ],
    useCases: [
      {
        title: 'Executive Advisory',
        description: 'Get CFO, COO, and Strategy perspectives on business decisions with agents configured to think like your leadership team.',
        industry: 'Leadership'
      },
      {
        title: 'Due Diligence',
        description: 'Have specialized agents analyze different aspects of a target company - financial, operational, legal, HR.',
        industry: 'Private Equity'
      },
      {
        title: 'Problem Solving',
        description: 'Multi-agent brainstorming sessions where agents with different viewpoints collaborate on complex challenges.',
        industry: 'All Industries'
      }
    ],
    // Deep dive content for AI Agents page
    deepDive: {
      howItWorks: {
        title: 'How Multi-Agent AI Works',
        description: 'OrbitOS implements a sophisticated multi-agent architecture that enables specialized AI entities to collaborate on complex business problems. Unlike single-agent systems, our approach allows for diverse perspectives, checks and balances, and domain-specific expertise.',
        steps: [
          {
            title: '1. Agent Creation',
            description: 'You create agents with specific roles, personalities, and areas of expertise. Each agent has its own system prompt that defines how it thinks and responds.'
          },
          {
            title: '2. Context Injection',
            description: 'When a conversation starts, agents receive your organization\'s context - business model canvas, org structure, processes, roles, and more. This grounds their responses in your reality.'
          },
          {
            title: '3. Conversation Flow',
            description: 'You or other participants send messages. Agents can be invoked via @mentions or participate according to the conversation mode you\'ve selected.'
          },
          {
            title: '4. Collaborative Response',
            description: 'Agents respond based on their configuration, building on each other\'s insights. The CFO agent might analyze financials while the Operations agent considers implementation.'
          },
          {
            title: '5. Action Proposals',
            description: 'When agents identify actionable recommendations, they can propose changes to your data. These go through a pending action workflow for human approval.'
          }
        ]
      },
      agentConfiguration: {
        title: 'Agent Configuration Deep Dive',
        sections: [
          {
            title: 'Identity & Appearance',
            items: [
              { name: 'Name', description: 'The agent\'s display name (e.g., "CFO Advisor", "Operations Expert", "HR Partner")' },
              { name: 'Avatar Color', description: 'Choose from a palette of colors to visually distinguish agents in conversations' },
              { name: 'Description', description: 'A brief description of the agent\'s role and expertise for other users' }
            ]
          },
          {
            title: 'Model Selection',
            items: [
              { name: 'Claude Opus', description: 'Most capable model. Best for complex analysis, nuanced reasoning, and high-stakes decisions. Higher cost, slower response.' },
              { name: 'Claude Sonnet', description: 'Balanced model. Great for most business tasks - analysis, recommendations, and general advisory. Good cost/performance ratio.' },
              { name: 'Claude Haiku', description: 'Fastest and most affordable. Ideal for quick responses, simple tasks, and high-volume interactions.' }
            ]
          },
          {
            title: 'Generation Parameters',
            items: [
              { name: 'Temperature (0.0 - 1.0)', description: 'Controls response creativity. Low (0.1-0.3): Consistent, factual, conservative. Medium (0.4-0.7): Balanced creativity. High (0.8-1.0): Creative, varied, exploratory.' },
              { name: 'Max Tokens', description: 'Maximum response length. 1024 tokens ≈ 750 words. Set based on expected response complexity.' }
            ]
          }
        ]
      },
      systemPrompts: {
        title: 'System Prompts & Personality',
        description: 'The system prompt is the most powerful configuration tool. It defines who the agent is, how it thinks, and how it communicates. OrbitOS provides templates, but you can fully customize them.',
        templates: [
          {
            name: 'CFO Advisor',
            traits: ['Financial analysis focus', 'Risk-aware', 'ROI-driven', 'Budget-conscious'],
            examplePrompt: 'You are a seasoned CFO with 20+ years of experience in financial strategy and risk management. You analyze every decision through the lens of financial impact, ROI, and risk exposure. You ask clarifying questions about budget constraints and always consider the financial implications of recommendations.'
          },
          {
            name: 'Operations Expert',
            traits: ['Process-oriented', 'Efficiency-focused', 'Practical', 'Detail-oriented'],
            examplePrompt: 'You are an Operations expert who thinks in terms of processes, workflows, and efficiency. You identify bottlenecks, suggest process improvements, and always consider implementation feasibility. You ask about resource constraints and timelines.'
          },
          {
            name: 'Strategy Consultant',
            traits: ['Big-picture thinking', 'Competitive analysis', 'Long-term focus', 'Framework-driven'],
            examplePrompt: 'You are a Strategy consultant who thinks about competitive positioning, market dynamics, and long-term vision. You use frameworks like Porter\'s Five Forces, SWOT, and Blue Ocean Strategy. You challenge assumptions and look for strategic opportunities.'
          },
          {
            name: 'HR Partner',
            traits: ['People-focused', 'Culture-aware', 'Compliance-minded', 'Empathetic'],
            examplePrompt: 'You are an HR Business Partner who considers the human element in every decision. You think about team capacity, skills gaps, culture fit, and employee wellbeing. You flag potential compliance issues and consider change management implications.'
          }
        ],
        personalityTraits: {
          title: 'Personality Traits to Configure',
          traits: [
            {
              name: 'Assertiveness',
              description: 'How strongly the agent advocates for its viewpoint',
              levels: ['Deferential - Suggests gently, easily persuaded', 'Balanced - States opinions clearly, open to debate', 'Assertive - Strongly advocates, pushes back on disagreements']
            },
            {
              name: 'Communication Style',
              description: 'How the agent structures and delivers responses',
              levels: ['Concise - Brief, bullet-point focused', 'Balanced - Mix of detail and brevity', 'Comprehensive - Detailed explanations, thorough analysis']
            },
            {
              name: 'Risk Tolerance',
              description: 'How the agent approaches uncertainty and risk',
              levels: ['Conservative - Emphasizes caution, highlights risks', 'Moderate - Balanced risk assessment', 'Aggressive - Focuses on opportunities, downplays risks']
            },
            {
              name: 'Collaboration Style',
              description: 'How the agent interacts with other agents',
              levels: ['Supportive - Builds on others\' ideas', 'Independent - Offers distinct perspective', 'Challenging - Plays devil\'s advocate']
            }
          ]
        }
      },
      conversationModes: {
        title: 'Conversation Modes Explained',
        modes: [
          {
            name: 'On-Demand',
            description: 'Agents only respond when explicitly mentioned with @AgentName. This gives you full control over when each agent participates.',
            bestFor: 'Targeted questions, controlled discussions, specific expertise needed',
            example: 'You: "@CFO what are the financial implications of this expansion?" → Only CFO responds'
          },
          {
            name: 'Round-Robin',
            description: 'After each user message, agents take turns responding in a set order. Each agent gets a chance to contribute.',
            bestFor: 'Comprehensive analysis, ensuring all perspectives are heard, structured discussions',
            example: 'You: "Should we enter this market?" → CFO responds, then Operations, then Strategy, then HR'
          },
          {
            name: 'Moderated',
            description: 'Agents draft responses, but they\'re held for your approval before being added to the conversation. You can edit, approve, or reject.',
            bestFor: 'High-stakes discussions, quality control, sensitive topics',
            example: 'Agents draft responses → You review each → Approve/Edit/Reject → Approved messages appear'
          },
          {
            name: 'Free-Form',
            description: 'Agents can respond to each other and engage in natural back-and-forth discussion. Most dynamic but least controlled.',
            bestFor: 'Brainstorming, creative problem-solving, exploring ideas',
            example: 'CFO raises concern → Operations responds → Strategy adds perspective → Natural conversation emerges'
          }
        ]
      },
      dataAccess: {
        title: 'What Agents Can See & Do',
        sections: [
          {
            title: 'Read Access (Automatic)',
            items: [
              'Organization profile and settings',
              'Business Model Canvas (all 9 blocks)',
              'Organizational chart and reporting relationships',
              'Roles, functions, and assignments',
              'People and resource allocation',
              'Processes and activities',
              'Partners, channels, and value propositions'
            ]
          },
          {
            title: 'Proposed Actions (Requires Approval)',
            items: [
              'Create new entities (roles, processes, etc.)',
              'Update existing records',
              'Assign people to roles',
              'Modify process activities',
              'Update canvas block content'
            ]
          },
          {
            title: 'Never Allowed',
            items: [
              'Direct database modifications',
              'Access to other organizations',
              'Deletion of critical data',
              'Changes without audit trail'
            ]
          }
        ]
      },
      bestPractices: {
        title: 'Best Practices for Agent Design',
        practices: [
          {
            title: 'Create Complementary Agents',
            description: 'Design agents with different perspectives that balance each other. A risk-averse CFO paired with an opportunity-focused Strategy consultant creates productive tension.'
          },
          {
            title: 'Be Specific in System Prompts',
            description: 'Vague prompts lead to generic responses. Include specific frameworks, methodologies, and examples of how the agent should think.'
          },
          {
            title: 'Match Model to Task',
            description: 'Use Opus for complex analysis and important decisions. Use Sonnet for daily advisory. Use Haiku for quick checks and simple questions.'
          },
          {
            title: 'Set Appropriate Temperature',
            description: 'Lower temperature (0.2-0.4) for factual analysis and consistent advice. Higher temperature (0.6-0.8) for brainstorming and creative solutions.'
          },
          {
            title: 'Start with On-Demand Mode',
            description: 'Begin with On-Demand mode to learn how agents respond, then experiment with Round-Robin or Free-Form for collaborative discussions.'
          },
          {
            title: 'Review Pending Actions',
            description: 'Always review AI-proposed changes carefully. The pending action workflow exists to keep humans in control of actual data modifications.'
          }
        ]
      }
    }
  },
  'analytics': {
    title: 'Real-time Insights',
    subtitle: 'Dashboards & AI-Generated Insights',
    description: 'Get a complete view of your business with real-time dashboards, AI-generated insights, and actionable recommendations. Never miss a critical issue with intelligent alerting.',
    icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z',
    color: 'amber',
    benefits: [
      {
        title: 'Executive Dashboard',
        description: 'Key metrics across strategy, operations, and people at a glance.',
        icon: 'M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5z'
      },
      {
        title: 'AI Insights',
        description: 'Proactive recommendations based on data patterns and anomalies.',
        icon: 'M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707'
      },
      {
        title: 'Pending Actions',
        description: 'Track AI-proposed changes awaiting human approval.',
        icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z'
      },
      {
        title: 'Trend Analysis',
        description: 'Historical data visualization and forecasting capabilities.',
        icon: 'M13 7h8m0 0v8m0-8l-8 8-4-4-6 6'
      }
    ],
    capabilities: [
      {
        title: 'Metrics',
        items: ['Organizational coverage', 'Process efficiency', 'Role utilization', 'Capacity status', 'AI interaction stats']
      },
      {
        title: 'Visualizations',
        items: ['Summary cards', 'Charts & graphs', 'Status indicators', 'Trend lines', 'Heat maps']
      },
      {
        title: 'Alerts',
        items: ['Coverage gaps', 'Overloaded resources', 'Process bottlenecks', 'Pending approvals']
      }
    ],
    useCases: [
      {
        title: 'Executive Review',
        description: 'Weekly leadership meetings with data-driven insights.',
        industry: 'Leadership'
      },
      {
        title: 'Operations Monitoring',
        description: 'Real-time visibility into operational health and issues.',
        industry: 'Operations'
      },
      {
        title: 'Board Reporting',
        description: 'Generate executive summaries for board presentations.',
        industry: 'Governance'
      }
    ]
  },
  'security': {
    title: 'Enterprise Security',
    subtitle: 'SOC 2 Compliant with Full Audit Trails',
    description: 'Built for enterprise from day one. SOC 2 compliant infrastructure, SSO integration, role-based access control, and complete audit trails for every AI interaction.',
    icon: 'M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z',
    color: 'rose',
    benefits: [
      {
        title: 'SOC 2 Type II',
        description: 'Certified compliance with industry security standards.',
        icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z'
      },
      {
        title: 'SSO Integration',
        description: 'Google, Microsoft Azure AD, and SAML authentication support.',
        icon: 'M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z'
      },
      {
        title: 'Access Control',
        description: 'Role-based permissions with granular control over data access.',
        icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z'
      },
      {
        title: 'Audit Trails',
        description: 'Complete logging of all user and AI actions for compliance.',
        icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2'
      }
    ],
    capabilities: [
      {
        title: 'Authentication',
        items: ['Email/password', 'Google OAuth', 'Microsoft Azure AD', 'SAML 2.0 (Enterprise)']
      },
      {
        title: 'Authorization',
        items: ['Role-based access', 'Organization scoping', 'Multi-tenancy isolation', 'API key management']
      },
      {
        title: 'Compliance',
        items: ['SOC 2 Type II', 'GDPR ready', 'Data encryption', 'Audit logging', 'Data retention policies']
      }
    ],
    useCases: [
      {
        title: 'Regulated Industries',
        description: 'Meet compliance requirements for financial services and healthcare.',
        industry: 'Finance & Healthcare'
      },
      {
        title: 'Enterprise Deployment',
        description: 'Integrate with existing SSO and security infrastructure.',
        industry: 'Enterprise IT'
      },
      {
        title: 'AI Governance',
        description: 'Full audit trail of AI interactions for responsible AI practices.',
        industry: 'All Industries'
      }
    ]
  }
}

const slug = computed(() => route.params.slug as string)
const feature = computed(() => featuresData[slug.value])

// 404 handling
if (!feature.value) {
  throw createError({
    statusCode: 404,
    statusMessage: 'Feature Not Found'
  })
}

// Color classes helper
const getColorClasses = (color: string) => ({
  bg: color === 'purple' ? 'bg-purple-500/20' :
      color === 'blue' ? 'bg-blue-500/20' :
      color === 'emerald' ? 'bg-emerald-500/20' :
      color === 'cyan' ? 'bg-cyan-500/20' :
      color === 'amber' ? 'bg-amber-500/20' :
      'bg-rose-500/20',
  text: color === 'purple' ? 'text-purple-400' :
        color === 'blue' ? 'text-blue-400' :
        color === 'emerald' ? 'text-emerald-400' :
        color === 'cyan' ? 'text-cyan-400' :
        color === 'amber' ? 'text-amber-400' :
        'text-rose-400',
  border: color === 'purple' ? 'border-purple-500/30' :
          color === 'blue' ? 'border-blue-500/30' :
          color === 'emerald' ? 'border-emerald-500/30' :
          color === 'cyan' ? 'border-cyan-500/30' :
          color === 'amber' ? 'border-amber-500/30' :
          'border-rose-500/30',
  gradient: color === 'purple' ? 'from-purple-600 to-purple-400' :
            color === 'blue' ? 'from-blue-600 to-blue-400' :
            color === 'emerald' ? 'from-emerald-600 to-emerald-400' :
            color === 'cyan' ? 'from-cyan-600 to-cyan-400' :
            color === 'amber' ? 'from-amber-600 to-amber-400' :
            'from-rose-600 to-rose-400'
})

const colorClasses = computed(() => getColorClasses(feature.value?.color || 'purple'))

useHead({
  title: `${feature.value?.title} | OrbitOS`,
  meta: [
    { name: 'description', content: feature.value?.description }
  ]
})
</script>

<template>
  <div class="min-h-screen bg-slate-950">
    <!-- Animated Background -->
    <div class="fixed inset-0 overflow-hidden pointer-events-none">
      <div class="absolute top-1/4 left-1/4 w-[500px] h-[500px] bg-purple-500/20 rounded-full blur-[120px] animate-pulse"></div>
      <div class="absolute bottom-1/4 right-1/4 w-[400px] h-[400px] bg-blue-500/20 rounded-full blur-[100px] animate-pulse" style="animation-delay: 1s;"></div>
    </div>

    <!-- Navigation -->
    <nav class="fixed top-0 left-0 right-0 z-50 backdrop-blur-xl bg-slate-950/80 border-b border-white/5">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between h-16">
          <!-- Logo -->
          <NuxtLink to="/" class="flex items-center gap-3">
            <div class="flex items-center justify-center w-10 h-10 rounded-xl bg-gradient-to-br from-purple-500 to-blue-600 shadow-lg shadow-purple-500/20">
              <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3c.132 0 .263 0 .393 0a7.5 7.5 0 0 0 7.92 12.446a9 9 0 1 1 -8.313-12.454z" />
              </svg>
            </div>
            <span class="text-xl font-bold text-white">OrbitOS</span>
          </NuxtLink>

          <!-- Back to Home -->
          <div class="flex items-center gap-4">
            <NuxtLink to="/" class="text-white/60 hover:text-white transition-colors text-sm font-medium flex items-center gap-1">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
              </svg>
              Back to Home
            </NuxtLink>
            <NuxtLink
              v-if="!isLoading && isAuthenticated"
              to="/app"
              class="px-4 py-2 text-sm font-medium text-white bg-gradient-to-r from-purple-600 to-blue-600 rounded-lg hover:shadow-lg hover:shadow-purple-500/30 transition-all"
            >
              Go to App
            </NuxtLink>
            <NuxtLink
              v-else-if="!isLoading"
              to="/login"
              class="px-4 py-2 text-sm font-medium text-white bg-gradient-to-r from-purple-600 to-blue-600 rounded-lg hover:shadow-lg hover:shadow-purple-500/30 transition-all"
            >
              Start Free Trial
            </NuxtLink>
          </div>
        </div>
      </div>
    </nav>

    <!-- Hero Section -->
    <section class="relative pt-32 pb-20 px-4 sm:px-6 lg:px-8">
      <div class="max-w-7xl mx-auto">
        <div class="text-center max-w-4xl mx-auto">
          <!-- Feature Icon -->
          <div
            :class="[
              'inline-flex items-center justify-center w-20 h-20 rounded-2xl mb-8',
              colorClasses.bg
            ]"
          >
            <svg :class="['w-10 h-10', colorClasses.text]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="feature?.icon" />
            </svg>
          </div>

          <!-- Title -->
          <h1 class="text-4xl sm:text-5xl lg:text-6xl font-bold text-white tracking-tight mb-4">
            {{ feature?.title }}
          </h1>

          <!-- Subtitle -->
          <p :class="['text-xl sm:text-2xl mb-6', colorClasses.text]">
            {{ feature?.subtitle }}
          </p>

          <!-- Description -->
          <p class="text-lg text-white/60 max-w-3xl mx-auto mb-10">
            {{ feature?.description }}
          </p>

          <!-- CTA Buttons -->
          <div class="flex flex-col sm:flex-row items-center justify-center gap-4">
            <NuxtLink
              to="/login"
              class="group flex items-center gap-2 px-8 py-4 text-lg font-semibold text-white bg-gradient-to-r from-purple-600 to-blue-600 rounded-2xl hover:shadow-xl hover:shadow-purple-500/30 transition-all hover:scale-105"
            >
              Try It Free
              <svg class="w-5 h-5 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
              </svg>
            </NuxtLink>
            <a
              href="mailto:sales@orbitos.io"
              class="flex items-center gap-2 px-8 py-4 text-lg font-semibold text-white/80 bg-white/5 border border-white/10 rounded-2xl hover:bg-white/10 transition-all"
            >
              Request Demo
            </a>
          </div>
        </div>
      </div>
    </section>

    <!-- Benefits Section -->
    <section class="relative py-24 px-4 sm:px-6 lg:px-8">
      <div class="max-w-7xl mx-auto">
        <h2 class="text-3xl sm:text-4xl font-bold text-white text-center mb-16">
          Key Benefits
        </h2>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div
            v-for="benefit in feature?.benefits"
            :key="benefit.title"
            class="backdrop-blur-sm bg-white/5 border border-white/10 rounded-2xl p-6 hover:bg-white/10 transition-all"
          >
            <div
              :class="[
                'inline-flex items-center justify-center w-12 h-12 rounded-xl mb-4',
                colorClasses.bg
              ]"
            >
              <svg :class="['w-6 h-6', colorClasses.text]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="benefit.icon" />
              </svg>
            </div>
            <h3 class="text-xl font-semibold text-white mb-2">{{ benefit.title }}</h3>
            <p class="text-white/60">{{ benefit.description }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- Capabilities Section -->
    <section class="relative py-24 px-4 sm:px-6 lg:px-8 bg-white/[0.02]">
      <div class="max-w-7xl mx-auto">
        <h2 class="text-3xl sm:text-4xl font-bold text-white text-center mb-16">
          Capabilities
        </h2>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div
            v-for="capability in feature?.capabilities"
            :key="capability.title"
            :class="[
              'backdrop-blur-sm bg-white/5 border rounded-2xl p-6',
              colorClasses.border
            ]"
          >
            <h3 :class="['text-lg font-semibold mb-4', colorClasses.text]">{{ capability.title }}</h3>
            <ul class="space-y-3">
              <li
                v-for="item in capability.items"
                :key="item"
                class="flex items-start gap-2 text-white/70"
              >
                <svg :class="['w-5 h-5 flex-shrink-0 mt-0.5', colorClasses.text]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
                {{ item }}
              </li>
            </ul>
          </div>
        </div>
      </div>
    </section>

    <!-- Use Cases Section -->
    <section class="relative py-24 px-4 sm:px-6 lg:px-8">
      <div class="max-w-7xl mx-auto">
        <h2 class="text-3xl sm:text-4xl font-bold text-white text-center mb-16">
          Use Cases
        </h2>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div
            v-for="useCase in feature?.useCases"
            :key="useCase.title"
            class="backdrop-blur-sm bg-white/5 border border-white/10 rounded-2xl p-6 hover:bg-white/10 transition-all"
          >
            <div :class="['text-xs font-medium px-2 py-1 rounded-full inline-block mb-4', colorClasses.bg, colorClasses.text]">
              {{ useCase.industry }}
            </div>
            <h3 class="text-xl font-semibold text-white mb-2">{{ useCase.title }}</h3>
            <p class="text-white/60">{{ useCase.description }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- AI Agents Deep Dive Section (only shown for ai-agents) -->
    <template v-if="slug === 'ai-agents' && (feature as any)?.deepDive">
      <!-- How It Works -->
      <section class="relative py-24 px-4 sm:px-6 lg:px-8 bg-white/[0.02]">
        <div class="max-w-7xl mx-auto">
          <h2 class="text-3xl sm:text-4xl font-bold text-white text-center mb-4">
            {{ (feature as any).deepDive.howItWorks.title }}
          </h2>
          <p class="text-lg text-white/60 text-center max-w-3xl mx-auto mb-16">
            {{ (feature as any).deepDive.howItWorks.description }}
          </p>

          <div class="relative">
            <!-- Connection line -->
            <div class="absolute left-8 top-8 bottom-8 w-0.5 bg-gradient-to-b from-cyan-500/50 via-cyan-500/30 to-cyan-500/10 hidden md:block"></div>

            <div class="space-y-8">
              <div
                v-for="(step, index) in (feature as any).deepDive.howItWorks.steps"
                :key="step.title"
                class="relative flex gap-6"
              >
                <!-- Step number -->
                <div class="flex-shrink-0 w-16 h-16 rounded-2xl bg-cyan-500/20 border border-cyan-500/30 flex items-center justify-center text-cyan-400 font-bold text-xl z-10">
                  {{ index + 1 }}
                </div>
                <!-- Step content -->
                <div class="flex-1 backdrop-blur-sm bg-white/5 border border-white/10 rounded-2xl p-6">
                  <h3 class="text-xl font-semibold text-white mb-2">{{ step.title }}</h3>
                  <p class="text-white/60">{{ step.description }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- Agent Configuration -->
      <section class="relative py-24 px-4 sm:px-6 lg:px-8">
        <div class="max-w-7xl mx-auto">
          <h2 class="text-3xl sm:text-4xl font-bold text-white text-center mb-16">
            {{ (feature as any).deepDive.agentConfiguration.title }}
          </h2>

          <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div
              v-for="section in (feature as any).deepDive.agentConfiguration.sections"
              :key="section.title"
              class="backdrop-blur-sm bg-white/5 border border-cyan-500/30 rounded-2xl p-6"
            >
              <h3 class="text-lg font-semibold text-cyan-400 mb-6">{{ section.title }}</h3>
              <div class="space-y-4">
                <div
                  v-for="item in section.items"
                  :key="item.name"
                  class="border-b border-white/5 pb-4 last:border-0 last:pb-0"
                >
                  <h4 class="text-white font-medium mb-1">{{ item.name }}</h4>
                  <p class="text-white/50 text-sm">{{ item.description }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- System Prompts & Personality -->
      <section class="relative py-24 px-4 sm:px-6 lg:px-8 bg-white/[0.02]">
        <div class="max-w-7xl mx-auto">
          <h2 class="text-3xl sm:text-4xl font-bold text-white text-center mb-4">
            {{ (feature as any).deepDive.systemPrompts.title }}
          </h2>
          <p class="text-lg text-white/60 text-center max-w-3xl mx-auto mb-16">
            {{ (feature as any).deepDive.systemPrompts.description }}
          </p>

          <!-- Agent Templates -->
          <h3 class="text-2xl font-bold text-white mb-8">Agent Templates</h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-16">
            <div
              v-for="template in (feature as any).deepDive.systemPrompts.templates"
              :key="template.name"
              class="backdrop-blur-sm bg-white/5 border border-white/10 rounded-2xl p-6 hover:border-cyan-500/30 transition-all"
            >
              <h4 class="text-xl font-semibold text-white mb-3">{{ template.name }}</h4>
              <div class="flex flex-wrap gap-2 mb-4">
                <span
                  v-for="trait in template.traits"
                  :key="trait"
                  class="text-xs px-2 py-1 rounded-full bg-cyan-500/20 text-cyan-400"
                >
                  {{ trait }}
                </span>
              </div>
              <div class="bg-slate-900/50 rounded-xl p-4 border border-white/5">
                <p class="text-white/50 text-sm italic">"{{ template.examplePrompt }}"</p>
              </div>
            </div>
          </div>

          <!-- Personality Traits -->
          <h3 class="text-2xl font-bold text-white mb-4">{{ (feature as any).deepDive.systemPrompts.personalityTraits.title }}</h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div
              v-for="trait in (feature as any).deepDive.systemPrompts.personalityTraits.traits"
              :key="trait.name"
              class="backdrop-blur-sm bg-white/5 border border-white/10 rounded-2xl p-6"
            >
              <h4 class="text-xl font-semibold text-white mb-2">{{ trait.name }}</h4>
              <p class="text-white/50 text-sm mb-4">{{ trait.description }}</p>
              <div class="space-y-2">
                <div
                  v-for="(level, levelIndex) in trait.levels"
                  :key="level"
                  class="flex items-center gap-3"
                >
                  <div class="flex gap-1">
                    <div
                      v-for="n in 3"
                      :key="n"
                      :class="[
                        'w-2 h-2 rounded-full',
                        n <= levelIndex + 1 ? 'bg-cyan-400' : 'bg-white/20'
                      ]"
                    ></div>
                  </div>
                  <span class="text-white/70 text-sm">{{ level }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- Conversation Modes -->
      <section class="relative py-24 px-4 sm:px-6 lg:px-8">
        <div class="max-w-7xl mx-auto">
          <h2 class="text-3xl sm:text-4xl font-bold text-white text-center mb-16">
            {{ (feature as any).deepDive.conversationModes.title }}
          </h2>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div
              v-for="mode in (feature as any).deepDive.conversationModes.modes"
              :key="mode.name"
              class="backdrop-blur-sm bg-white/5 border border-white/10 rounded-2xl p-6 hover:bg-white/10 transition-all"
            >
              <h3 class="text-xl font-semibold text-cyan-400 mb-2">{{ mode.name }}</h3>
              <p class="text-white/70 mb-4">{{ mode.description }}</p>
              <div class="mb-4">
                <span class="text-xs font-medium text-white/50 uppercase tracking-wide">Best For</span>
                <p class="text-white/60 text-sm mt-1">{{ mode.bestFor }}</p>
              </div>
              <div class="bg-slate-900/50 rounded-xl p-4 border border-white/5">
                <span class="text-xs font-medium text-white/50 uppercase tracking-wide">Example</span>
                <p class="text-white/50 text-sm mt-1 italic">{{ mode.example }}</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- Data Access -->
      <section class="relative py-24 px-4 sm:px-6 lg:px-8 bg-white/[0.02]">
        <div class="max-w-7xl mx-auto">
          <h2 class="text-3xl sm:text-4xl font-bold text-white text-center mb-16">
            {{ (feature as any).deepDive.dataAccess.title }}
          </h2>

          <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div
              v-for="(section, index) in (feature as any).deepDive.dataAccess.sections"
              :key="section.title"
              :class="[
                'backdrop-blur-sm border rounded-2xl p-6',
                index === 0 ? 'bg-emerald-500/10 border-emerald-500/30' :
                index === 1 ? 'bg-amber-500/10 border-amber-500/30' :
                'bg-rose-500/10 border-rose-500/30'
              ]"
            >
              <h3 :class="[
                'text-lg font-semibold mb-6',
                index === 0 ? 'text-emerald-400' :
                index === 1 ? 'text-amber-400' :
                'text-rose-400'
              ]">
                {{ section.title }}
              </h3>
              <ul class="space-y-3">
                <li
                  v-for="item in section.items"
                  :key="item"
                  class="flex items-start gap-2 text-white/70"
                >
                  <svg
                    :class="[
                      'w-5 h-5 flex-shrink-0 mt-0.5',
                      index === 0 ? 'text-emerald-400' :
                      index === 1 ? 'text-amber-400' :
                      'text-rose-400'
                    ]"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      v-if="index === 0"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M5 13l4 4L19 7"
                    />
                    <path
                      v-else-if="index === 1"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                    />
                    <path
                      v-else
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                  {{ item }}
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <!-- Best Practices -->
      <section class="relative py-24 px-4 sm:px-6 lg:px-8">
        <div class="max-w-7xl mx-auto">
          <h2 class="text-3xl sm:text-4xl font-bold text-white text-center mb-16">
            {{ (feature as any).deepDive.bestPractices.title }}
          </h2>

          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div
              v-for="(practice, index) in (feature as any).deepDive.bestPractices.practices"
              :key="practice.title"
              class="backdrop-blur-sm bg-white/5 border border-white/10 rounded-2xl p-6 hover:border-cyan-500/30 transition-all"
            >
              <div class="flex items-center gap-3 mb-4">
                <div class="w-8 h-8 rounded-lg bg-cyan-500/20 flex items-center justify-center text-cyan-400 font-bold text-sm">
                  {{ index + 1 }}
                </div>
                <h3 class="text-lg font-semibold text-white">{{ practice.title }}</h3>
              </div>
              <p class="text-white/60">{{ practice.description }}</p>
            </div>
          </div>
        </div>
      </section>
    </template>

    <!-- CTA Section -->
    <section class="relative py-24 px-4 sm:px-6 lg:px-8">
      <div class="max-w-4xl mx-auto">
        <div :class="['backdrop-blur-xl bg-gradient-to-r border border-white/10 rounded-3xl p-12 text-center', `from-${feature?.color}-500/20`, `to-${feature?.color}-600/10`]">
          <h2 class="text-3xl sm:text-4xl font-bold text-white mb-4">
            Ready to Get Started?
          </h2>
          <p class="text-lg text-white/60 mb-8 max-w-2xl mx-auto">
            Join hundreds of organizations using OrbitOS to transform their operations.
          </p>
          <div class="flex flex-col sm:flex-row items-center justify-center gap-4">
            <NuxtLink
              to="/login"
              class="group flex items-center gap-2 px-8 py-4 text-lg font-semibold text-white bg-gradient-to-r from-purple-600 to-blue-600 rounded-2xl hover:shadow-xl hover:shadow-purple-500/30 transition-all"
            >
              Start Free Trial
              <svg class="w-5 h-5 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
              </svg>
            </NuxtLink>
            <NuxtLink
              to="/"
              class="flex items-center gap-2 px-8 py-4 text-lg font-semibold text-white/80 hover:text-white transition-colors"
            >
              Explore More Features
            </NuxtLink>
          </div>
        </div>
      </div>
    </section>

    <!-- Footer -->
    <footer class="relative py-8 px-4 sm:px-6 lg:px-8 border-t border-white/5">
      <div class="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between">
        <div class="flex items-center gap-3 mb-4 sm:mb-0">
          <div class="flex items-center justify-center w-8 h-8 rounded-lg bg-gradient-to-br from-purple-500 to-blue-600">
            <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3c.132 0 .263 0 .393 0a7.5 7.5 0 0 0 7.92 12.446a9 9 0 1 1 -8.313-12.454z" />
            </svg>
          </div>
          <span class="text-white/60 text-sm">&copy; 2026 OrbitOS. All rights reserved.</span>
        </div>
        <div class="flex items-center gap-6">
          <NuxtLink to="/" class="text-white/40 hover:text-white transition-colors text-sm">Home</NuxtLink>
          <NuxtLink to="/login" class="text-white/40 hover:text-white transition-colors text-sm">Login</NuxtLink>
        </div>
      </div>
    </footer>
  </div>
</template>
