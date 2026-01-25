<script setup lang="ts">
import { useAuth } from '~/composables/useAuth'

const { isAuthenticated, isLoading, initializeAuth } = useAuth()

onMounted(() => {
  initializeAuth()
})

// Mobile menu state
const showMobileMenu = ref(false)

// Scroll to section
const scrollToSection = (id: string) => {
  const element = document.getElementById(id)
  if (element) {
    element.scrollIntoView({ behavior: 'smooth' })
  }
  showMobileMenu.value = false
}

// Features dropdown state
const showFeaturesDropdown = ref(false)
let dropdownTimeout: ReturnType<typeof setTimeout> | null = null

const openFeaturesDropdown = () => {
  if (dropdownTimeout) clearTimeout(dropdownTimeout)
  showFeaturesDropdown.value = true
}

const closeFeaturesDropdown = () => {
  dropdownTimeout = setTimeout(() => {
    showFeaturesDropdown.value = false
  }, 150)
}

// Features data with slugs for routing
const features = [
  {
    slug: 'business-model-canvas',
    icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2',
    title: 'Business Model Canvas',
    shortDescription: 'Strategic planning with connected canvases',
    description: 'Visualize and manage your entire business model with interconnected canvases linked to real operational data.',
    color: 'purple'
  },
  {
    slug: 'organizational-intelligence',
    icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z',
    title: 'Organizational Intelligence',
    shortDescription: 'Org charts, roles & capacity planning',
    description: 'Map your org chart, roles, and functions. AI identifies coverage gaps, single points of failure, and capacity issues.',
    color: 'blue'
  },
  {
    slug: 'process-automation',
    icon: 'M13 10V3L4 14h7v7l9-11h-7z',
    title: 'Process Automation',
    shortDescription: 'Visual process design & optimization',
    description: 'Design, document, and optimize business processes with visual flow editors and AI-powered recommendations.',
    color: 'emerald'
  },
  {
    slug: 'ai-agents',
    icon: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z',
    title: 'Multi-Agent AI Chat',
    shortDescription: 'Specialized AI agents for your business',
    description: 'Collaborate with specialized AI agents - CFO, Operations, Strategy, HR - that understand your business context.',
    color: 'cyan'
  },
  {
    slug: 'analytics',
    icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z',
    title: 'Real-time Insights',
    shortDescription: 'Dashboards & AI-generated insights',
    description: 'Dashboard with actionable metrics, pending actions, and AI-generated insights across all business dimensions.',
    color: 'amber'
  },
  {
    slug: 'security',
    icon: 'M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z',
    title: 'Enterprise Security',
    shortDescription: 'SOC 2 compliant with full audit trails',
    description: 'SOC 2 compliant, SSO integration, role-based access control, and complete audit trails for all AI interactions.',
    color: 'rose'
  }
]

// Integration logos data
const integrations = [
  { name: 'Slack', icon: 'slack' },
  { name: 'Microsoft 365', icon: 'microsoft' },
  { name: 'Google Workspace', icon: 'google' },
  { name: 'Salesforce', icon: 'salesforce' },
  { name: 'Notion', icon: 'notion' },
  { name: 'Jira', icon: 'jira' }
]

// Metrics/outcomes data
const metrics = [
  { value: '50%', label: 'Faster Strategic Planning', description: 'Reduce planning cycles with connected data' },
  { value: '3x', label: 'Better Risk Visibility', description: 'AI identifies gaps before they become problems' },
  { value: '40%', label: 'Less Context Switching', description: 'One platform for strategy, ops, and people' }
]

// Testimonials data
const testimonials = [
  {
    quote: "OrbitOS helped us identify three critical single points of failure in our org structure that we'd missed for years. The AI recommendations were spot-on.",
    author: "Sarah Chen",
    title: "COO",
    company: "TechScale Inc.",
    avatar: "SC"
  },
  {
    quote: "Finally, a tool that connects our business model canvas to actual operational data. We can see the impact of strategic decisions in real-time.",
    author: "Marcus Rodriguez",
    title: "VP of Operations",
    company: "GrowthPath Partners",
    avatar: "MR"
  },
  {
    quote: "The multi-agent AI chat is a game changer. We have specialized agents for finance, HR, and strategy that actually understand our business context.",
    author: "Jennifer Walsh",
    title: "Director of Strategy",
    company: "Nexus Consulting",
    avatar: "JW"
  }
]

// Pricing tiers
const pricing = [
  {
    name: 'Starter',
    price: '99',
    description: 'For small teams getting started',
    features: [
      'Up to 10 team members',
      '1 Business Model Canvas',
      'Basic org chart',
      'Process documentation',
      'Email support'
    ],
    cta: 'Start Free Trial',
    highlighted: false
  },
  {
    name: 'Business',
    price: '299',
    description: 'For growing organizations',
    features: [
      'Up to 50 team members',
      'Unlimited canvases',
      'AI agent conversations',
      'Advanced analytics',
      'SSO integration',
      'Priority support'
    ],
    cta: 'Start Free Trial',
    highlighted: true
  },
  {
    name: 'Enterprise',
    price: 'Custom',
    description: 'For large organizations',
    features: [
      'Unlimited team members',
      'Custom AI agents',
      'API access',
      'Dedicated success manager',
      'Custom integrations',
      'SLA guarantee'
    ],
    cta: 'Contact Sales',
    highlighted: false
  }
]
</script>

<template>
  <div class="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 relative overflow-x-hidden">
    <!-- Animated background elements -->
    <div class="fixed inset-0 overflow-hidden pointer-events-none">
      <div class="absolute -top-40 -right-40 bg-purple-600 rounded-full mix-blend-multiply filter blur-[128px] opacity-20 animate-blob" style="width: 600px; height: 600px;"></div>
      <div class="absolute -bottom-40 -left-40 bg-blue-600 rounded-full mix-blend-multiply filter blur-[128px] opacity-20 animate-blob animation-delay-2000" style="width: 600px; height: 600px;"></div>
      <div class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-indigo-600 rounded-full mix-blend-multiply filter blur-[128px] opacity-10 animate-blob animation-delay-4000" style="width: 800px; height: 800px;"></div>
    </div>

    <!-- Navigation -->
    <nav class="fixed top-0 left-0 right-0 z-50 backdrop-blur-xl bg-slate-950/70 border-b border-white/5">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between h-16">
          <!-- Logo -->
          <div class="flex items-center gap-3">
            <div class="flex items-center justify-center w-10 h-10 rounded-xl bg-gradient-to-br from-purple-500 to-blue-600 shadow-lg shadow-purple-500/20">
              <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3c.132 0 .263 0 .393 0a7.5 7.5 0 0 0 7.92 12.446a9 9 0 1 1 -8.313-12.454z" />
              </svg>
            </div>
            <span class="text-xl font-bold text-white">OrbitOS</span>
          </div>

          <!-- Mobile menu button -->
          <button
            class="md:hidden p-2 rounded-lg text-white/60 hover:text-white hover:bg-white/5 transition-colors"
            @click="showMobileMenu = !showMobileMenu"
          >
            <svg v-if="!showMobileMenu" class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
            <svg v-else class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>

          <!-- Nav Links (Desktop) -->
          <div class="hidden md:flex items-center gap-8">
            <!-- Features Dropdown -->
            <div
              class="relative"
              @mouseenter="openFeaturesDropdown"
              @mouseleave="closeFeaturesDropdown"
            >
              <button class="flex items-center gap-1 text-white/60 hover:text-white transition-colors text-sm font-medium">
                Features
                <svg class="w-4 h-4 transition-transform" :class="{ 'rotate-180': showFeaturesDropdown }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                </svg>
              </button>

              <!-- Dropdown Menu -->
              <Transition
                enter-active-class="transition duration-200 ease-out"
                enter-from-class="opacity-0 translate-y-1"
                enter-to-class="opacity-100 translate-y-0"
                leave-active-class="transition duration-150 ease-in"
                leave-from-class="opacity-100 translate-y-0"
                leave-to-class="opacity-0 translate-y-1"
              >
                <div
                  v-if="showFeaturesDropdown"
                  class="absolute top-full left-1/2 -translate-x-1/2 pt-4 w-[480px]"
                  @mouseenter="openFeaturesDropdown"
                  @mouseleave="closeFeaturesDropdown"
                >
                  <div class="backdrop-blur-xl bg-slate-900/95 border border-white/10 rounded-2xl shadow-2xl shadow-black/50 overflow-hidden">
                    <div class="p-2">
                      <NuxtLink
                        v-for="feature in features"
                        :key="feature.slug"
                        :to="`/features/${feature.slug}`"
                        class="flex items-start gap-4 p-3 rounded-xl hover:bg-white/5 transition-colors group"
                      >
                        <div
                          :class="[
                            'flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center',
                            feature.color === 'purple' ? 'bg-purple-500/20 text-purple-400' : '',
                            feature.color === 'blue' ? 'bg-blue-500/20 text-blue-400' : '',
                            feature.color === 'emerald' ? 'bg-emerald-500/20 text-emerald-400' : '',
                            feature.color === 'cyan' ? 'bg-cyan-500/20 text-cyan-400' : '',
                            feature.color === 'amber' ? 'bg-amber-500/20 text-amber-400' : '',
                            feature.color === 'rose' ? 'bg-rose-500/20 text-rose-400' : ''
                          ]"
                        >
                          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="feature.icon" />
                          </svg>
                        </div>
                        <div class="flex-1 min-w-0">
                          <div class="text-sm font-medium text-white group-hover:text-white transition-colors">
                            {{ feature.title }}
                          </div>
                          <div class="text-xs text-white/50 mt-0.5">
                            {{ feature.shortDescription }}
                          </div>
                        </div>
                        <svg class="w-4 h-4 text-white/30 group-hover:text-white/60 group-hover:translate-x-0.5 transition-all mt-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                        </svg>
                      </NuxtLink>
                    </div>

                    <!-- Footer -->
                    <div class="border-t border-white/5 p-3 bg-white/[0.02]">
                      <button
                        @click="scrollToSection('features'); showFeaturesDropdown = false"
                        class="w-full text-center text-sm text-white/50 hover:text-white transition-colors"
                      >
                        View all features overview
                      </button>
                    </div>
                  </div>
                </div>
              </Transition>
            </div>

            <button @click="scrollToSection('testimonials')" class="text-white/60 hover:text-white transition-colors text-sm font-medium">Customers</button>
            <button @click="scrollToSection('pricing')" class="text-white/60 hover:text-white transition-colors text-sm font-medium">Pricing</button>
          </div>

          <!-- Auth Buttons (Desktop) -->
          <div class="hidden md:flex items-center gap-3">
            <NuxtLink
              v-if="!isLoading && isAuthenticated"
              to="/app"
              class="px-4 py-2 text-sm font-medium text-white bg-gradient-to-r from-purple-600 to-blue-600 rounded-lg hover:shadow-lg hover:shadow-purple-500/30 transition-all"
            >
              Go to App
            </NuxtLink>
            <template v-else-if="!isLoading">
              <NuxtLink
                to="/login"
                class="px-4 py-2 text-sm font-medium text-white/70 hover:text-white transition-colors"
              >
                Log In
              </NuxtLink>
              <NuxtLink
                to="/login"
                class="px-4 py-2 text-sm font-medium text-white bg-gradient-to-r from-purple-600 to-blue-600 rounded-lg hover:shadow-lg hover:shadow-purple-500/30 transition-all"
              >
                Start Free Trial
              </NuxtLink>
            </template>
          </div>
        </div>

        <!-- Mobile Menu -->
        <Transition
          enter-active-class="transition duration-200 ease-out"
          enter-from-class="opacity-0 -translate-y-2"
          enter-to-class="opacity-100 translate-y-0"
          leave-active-class="transition duration-150 ease-in"
          leave-from-class="opacity-100 translate-y-0"
          leave-to-class="opacity-0 -translate-y-2"
        >
          <div v-if="showMobileMenu" class="md:hidden py-4 border-t border-white/5">
            <div class="space-y-2">
              <button
                @click="scrollToSection('features')"
                class="block w-full text-left px-4 py-2 text-white/70 hover:text-white hover:bg-white/5 rounded-lg transition-colors"
              >
                Features
              </button>
              <button
                @click="scrollToSection('testimonials')"
                class="block w-full text-left px-4 py-2 text-white/70 hover:text-white hover:bg-white/5 rounded-lg transition-colors"
              >
                Customers
              </button>
              <button
                @click="scrollToSection('pricing')"
                class="block w-full text-left px-4 py-2 text-white/70 hover:text-white hover:bg-white/5 rounded-lg transition-colors"
              >
                Pricing
              </button>
              <div class="pt-2 border-t border-white/5 mt-2">
                <NuxtLink
                  v-if="!isLoading && isAuthenticated"
                  to="/app"
                  class="block w-full text-center px-4 py-2 text-white bg-gradient-to-r from-purple-600 to-blue-600 rounded-lg"
                  @click="showMobileMenu = false"
                >
                  Go to App
                </NuxtLink>
                <template v-else-if="!isLoading">
                  <NuxtLink
                    to="/login"
                    class="block w-full text-center px-4 py-2 text-white/70 hover:text-white transition-colors"
                    @click="showMobileMenu = false"
                  >
                    Log In
                  </NuxtLink>
                  <NuxtLink
                    to="/login"
                    class="block w-full text-center px-4 py-2 mt-2 text-white bg-gradient-to-r from-purple-600 to-blue-600 rounded-lg"
                    @click="showMobileMenu = false"
                  >
                    Start Free Trial
                  </NuxtLink>
                </template>
              </div>
            </div>
          </div>
        </Transition>
      </div>
    </nav>

    <!-- Hero Section -->
    <section class="relative pt-32 pb-20 px-4 sm:px-6 lg:px-8">
      <div class="max-w-7xl mx-auto text-center">
        <!-- Badge -->
        <div class="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white/5 border border-white/10 mb-8">
          <span class="flex h-2 w-2 rounded-full bg-emerald-400 animate-pulse"></span>
          <span class="text-sm text-white/60">AI-Native Business Operating System</span>
        </div>

        <!-- Headline -->
        <h1 class="text-4xl sm:text-5xl lg:text-7xl font-bold text-white tracking-tight mb-6">
          Connect Strategy to
          <br />
          <span class="bg-gradient-to-r from-purple-400 via-blue-400 to-cyan-400 text-transparent bg-clip-text">
            Execution
          </span>
        </h1>

        <!-- Subheadline with value prop -->
        <p class="text-lg sm:text-xl text-white/60 max-w-3xl mx-auto mb-10">
          OrbitOS unifies your business model, org structure, and processes in one intelligent platform.
          AI agents that understand your context help you identify risks, optimize operations, and make better decisions.
        </p>

        <!-- CTA Buttons -->
        <div class="flex flex-col sm:flex-row items-center justify-center gap-4 mb-12">
          <NuxtLink
            to="/login"
            class="group flex items-center gap-2 px-8 py-4 text-lg font-semibold text-white bg-gradient-to-r from-purple-600 to-blue-600 rounded-2xl hover:shadow-xl hover:shadow-purple-500/30 transition-all hover:scale-105"
          >
            Start Free Trial
            <svg class="w-5 h-5 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
            </svg>
          </NuxtLink>
          <button
            @click="scrollToSection('features')"
            class="flex items-center gap-2 px-8 py-4 text-lg font-semibold text-white/80 bg-white/5 border border-white/10 rounded-2xl hover:bg-white/10 transition-all"
          >
            See How It Works
          </button>
        </div>

        <!-- Metrics -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-6 max-w-3xl mx-auto mb-16">
          <div v-for="metric in metrics" :key="metric.label" class="text-center">
            <div class="text-3xl sm:text-4xl font-bold bg-gradient-to-r from-purple-400 to-blue-400 text-transparent bg-clip-text">
              {{ metric.value }}
            </div>
            <div class="text-white font-medium mt-1">{{ metric.label }}</div>
            <div class="text-white/40 text-sm mt-0.5">{{ metric.description }}</div>
          </div>
        </div>

        <!-- Hero Product Preview -->
        <div class="mt-8 relative">
          <div class="absolute inset-0 bg-gradient-to-t from-slate-950 via-transparent to-transparent z-10"></div>
          <div class="backdrop-blur-sm bg-white/5 border border-white/10 rounded-3xl p-4 shadow-2xl shadow-purple-500/10">
            <!-- Simulated Dashboard -->
            <div class="bg-slate-900 rounded-2xl overflow-hidden">
              <!-- Top bar -->
              <div class="h-10 bg-slate-800/50 border-b border-white/5 flex items-center px-4 gap-2">
                <div class="flex gap-1.5">
                  <div class="w-3 h-3 rounded-full bg-red-500/60"></div>
                  <div class="w-3 h-3 rounded-full bg-yellow-500/60"></div>
                  <div class="w-3 h-3 rounded-full bg-green-500/60"></div>
                </div>
                <div class="flex-1 flex justify-center">
                  <div class="px-3 py-1 bg-white/5 rounded text-xs text-white/40">OrbitOS Dashboard</div>
                </div>
              </div>

              <!-- Dashboard content -->
              <div class="p-6 grid grid-cols-12 gap-4" style="min-height: 400px;">
                <!-- Sidebar -->
                <div class="col-span-2 space-y-2">
                  <div class="h-8 bg-purple-500/20 rounded-lg"></div>
                  <div class="h-6 bg-white/5 rounded-lg w-3/4"></div>
                  <div class="h-6 bg-white/5 rounded-lg w-3/4"></div>
                  <div class="h-6 bg-white/10 rounded-lg w-3/4"></div>
                  <div class="h-6 bg-white/5 rounded-lg w-3/4"></div>
                  <div class="h-6 bg-white/5 rounded-lg w-3/4"></div>
                </div>

                <!-- Main content - Business Canvas -->
                <div class="col-span-7 bg-white/[0.02] rounded-xl border border-white/5 p-4">
                  <div class="flex items-center justify-between mb-4">
                    <div class="text-white/60 text-sm font-medium">Business Model Canvas</div>
                    <div class="flex gap-2">
                      <div class="px-2 py-1 bg-purple-500/20 rounded text-xs text-purple-400">Canvas View</div>
                      <div class="px-2 py-1 bg-white/5 rounded text-xs text-white/40">List</div>
                    </div>
                  </div>
                  <!-- 9-block canvas grid -->
                  <div class="grid grid-cols-5 gap-2 h-48">
                    <div class="row-span-2 bg-blue-500/10 rounded-lg p-2 border border-blue-500/20">
                      <div class="text-[10px] text-blue-400 font-medium">Partners</div>
                      <div class="mt-2 space-y-1">
                        <div class="h-3 bg-blue-500/20 rounded w-full"></div>
                        <div class="h-3 bg-blue-500/20 rounded w-2/3"></div>
                      </div>
                    </div>
                    <div class="bg-emerald-500/10 rounded-lg p-2 border border-emerald-500/20">
                      <div class="text-[10px] text-emerald-400 font-medium">Activities</div>
                    </div>
                    <div class="row-span-2 bg-purple-500/10 rounded-lg p-2 border border-purple-500/20">
                      <div class="text-[10px] text-purple-400 font-medium">Value Props</div>
                      <div class="mt-2 space-y-1">
                        <div class="h-3 bg-purple-500/20 rounded w-full"></div>
                        <div class="h-3 bg-purple-500/20 rounded w-3/4"></div>
                        <div class="h-3 bg-purple-500/20 rounded w-1/2"></div>
                      </div>
                    </div>
                    <div class="bg-cyan-500/10 rounded-lg p-2 border border-cyan-500/20">
                      <div class="text-[10px] text-cyan-400 font-medium">Relationships</div>
                    </div>
                    <div class="row-span-2 bg-amber-500/10 rounded-lg p-2 border border-amber-500/20">
                      <div class="text-[10px] text-amber-400 font-medium">Segments</div>
                      <div class="mt-2 space-y-1">
                        <div class="h-3 bg-amber-500/20 rounded w-full"></div>
                        <div class="h-3 bg-amber-500/20 rounded w-2/3"></div>
                      </div>
                    </div>
                    <div class="bg-emerald-500/10 rounded-lg p-2 border border-emerald-500/20">
                      <div class="text-[10px] text-emerald-400 font-medium">Resources</div>
                    </div>
                    <div class="bg-cyan-500/10 rounded-lg p-2 border border-cyan-500/20">
                      <div class="text-[10px] text-cyan-400 font-medium">Channels</div>
                    </div>
                    <div class="col-span-2 bg-rose-500/10 rounded-lg p-2 border border-rose-500/20">
                      <div class="text-[10px] text-rose-400 font-medium">Cost Structure</div>
                    </div>
                    <div class="col-span-3 bg-green-500/10 rounded-lg p-2 border border-green-500/20">
                      <div class="text-[10px] text-green-400 font-medium">Revenue Streams</div>
                    </div>
                  </div>
                </div>

                <!-- AI Panel -->
                <div class="col-span-3 bg-white/[0.02] rounded-xl border border-white/5 p-4 flex flex-col">
                  <div class="flex items-center gap-2 mb-4">
                    <div class="w-6 h-6 rounded-lg bg-purple-500/20 flex items-center justify-center">
                      <span class="text-purple-400 text-xs font-bold">AI</span>
                    </div>
                    <div class="text-white/60 text-sm font-medium">Strategy Agent</div>
                  </div>
                  <div class="flex-1 space-y-3">
                    <div class="bg-slate-800/50 rounded-lg rounded-tl-none p-3">
                      <p class="text-white/70 text-xs leading-relaxed">I noticed your "Customer Support" function has only 2 people assigned but handles 40% of customer touchpoints. This may indicate a capacity risk.</p>
                    </div>
                    <div class="bg-amber-500/10 border border-amber-500/20 rounded-lg p-3">
                      <div class="flex items-center gap-2 mb-1">
                        <svg class="w-4 h-4 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                        </svg>
                        <span class="text-amber-400 text-xs font-medium">Risk Detected</span>
                      </div>
                      <p class="text-white/60 text-xs">Single point of failure in Financial Analysis</p>
                    </div>
                  </div>
                  <div class="mt-3 flex gap-2">
                    <div class="flex-1 h-8 bg-white/5 rounded-lg border border-white/10 flex items-center px-3">
                      <span class="text-white/30 text-xs">Ask about your business...</span>
                    </div>
                    <div class="w-8 h-8 bg-purple-500/20 rounded-lg flex items-center justify-center">
                      <svg class="w-4 h-4 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
                      </svg>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Integration logos -->
        <div class="mt-16 text-center">
          <p class="text-white/40 text-sm mb-6">Integrates with your existing tools</p>
          <div class="flex flex-wrap justify-center items-center gap-8">
            <!-- Slack -->
            <div class="flex items-center gap-2 text-white/30 hover:text-white/50 transition-colors">
              <svg class="w-6 h-6" viewBox="0 0 24 24" fill="currentColor">
                <path d="M5.042 15.165a2.528 2.528 0 0 1-2.52 2.523A2.528 2.528 0 0 1 0 15.165a2.527 2.527 0 0 1 2.522-2.52h2.52v2.52zM6.313 15.165a2.527 2.527 0 0 1 2.521-2.52 2.527 2.527 0 0 1 2.521 2.52v6.313A2.528 2.528 0 0 1 8.834 24a2.528 2.528 0 0 1-2.521-2.522v-6.313zM8.834 5.042a2.528 2.528 0 0 1-2.521-2.52A2.528 2.528 0 0 1 8.834 0a2.528 2.528 0 0 1 2.521 2.522v2.52H8.834zM8.834 6.313a2.528 2.528 0 0 1 2.521 2.521 2.528 2.528 0 0 1-2.521 2.521H2.522A2.528 2.528 0 0 1 0 8.834a2.528 2.528 0 0 1 2.522-2.521h6.312zM18.956 8.834a2.528 2.528 0 0 1 2.522-2.521A2.528 2.528 0 0 1 24 8.834a2.528 2.528 0 0 1-2.522 2.521h-2.522V8.834zM17.688 8.834a2.528 2.528 0 0 1-2.523 2.521 2.527 2.527 0 0 1-2.52-2.521V2.522A2.527 2.527 0 0 1 15.165 0a2.528 2.528 0 0 1 2.523 2.522v6.312zM15.165 18.956a2.528 2.528 0 0 1 2.523 2.522A2.528 2.528 0 0 1 15.165 24a2.527 2.527 0 0 1-2.52-2.522v-2.522h2.52zM15.165 17.688a2.527 2.527 0 0 1-2.52-2.523 2.526 2.526 0 0 1 2.52-2.52h6.313A2.527 2.527 0 0 1 24 15.165a2.528 2.528 0 0 1-2.522 2.523h-6.313z"/>
              </svg>
              <span class="text-sm font-medium">Slack</span>
            </div>
            <!-- Microsoft -->
            <div class="flex items-center gap-2 text-white/30 hover:text-white/50 transition-colors">
              <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                <path d="M11.4 24H0V12.6h11.4V24zM24 24H12.6V12.6H24V24zM11.4 11.4H0V0h11.4v11.4zm12.6 0H12.6V0H24v11.4z"/>
              </svg>
              <span class="text-sm font-medium">Microsoft 365</span>
            </div>
            <!-- Google -->
            <div class="flex items-center gap-2 text-white/30 hover:text-white/50 transition-colors">
              <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
              </svg>
              <span class="text-sm font-medium">Google Workspace</span>
            </div>
            <!-- Notion -->
            <div class="flex items-center gap-2 text-white/30 hover:text-white/50 transition-colors">
              <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                <path d="M4.459 4.208c.746.606 1.026.56 2.428.466l13.215-.793c.28 0 .047-.28-.046-.326L17.86 1.968c-.42-.326-.98-.7-2.055-.607L3.01 2.295c-.466.046-.56.28-.374.466zm.793 3.08v13.904c0 .747.373 1.027 1.214.98l14.523-.84c.841-.046.935-.56.935-1.167V6.354c0-.606-.233-.933-.748-.887l-15.177.887c-.56.047-.747.327-.747.934zm14.337.745c.093.42 0 .84-.42.888l-.7.14v10.264c-.608.327-1.168.514-1.635.514-.748 0-.935-.234-1.495-.933l-4.577-7.186v6.952l1.449.327s0 .84-1.168.84l-3.222.186c-.093-.186 0-.653.327-.746l.84-.233V9.854L7.822 9.76c-.094-.42.14-1.026.793-1.073l3.456-.233 4.764 7.279v-6.44l-1.215-.14c-.093-.514.28-.887.747-.933zM1.936 1.035l13.31-.98c1.634-.14 2.055-.047 3.082.7l4.249 2.986c.7.513.934.653.934 1.213v16.378c0 1.026-.373 1.634-1.68 1.726l-15.458.934c-.98.047-1.448-.093-1.962-.747l-3.129-4.06c-.56-.747-.793-1.306-.793-1.96V2.667c0-.839.374-1.54 1.447-1.632z"/>
              </svg>
              <span class="text-sm font-medium">Notion</span>
            </div>
            <!-- Jira -->
            <div class="flex items-center gap-2 text-white/30 hover:text-white/50 transition-colors">
              <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                <path d="M11.571 11.513H0a5.218 5.218 0 0 0 5.232 5.215h2.13v2.057A5.215 5.215 0 0 0 12.575 24V12.518a1.005 1.005 0 0 0-1.005-1.005zm5.723-5.756H5.736a5.215 5.215 0 0 0 5.215 5.214h2.129v2.058a5.218 5.218 0 0 0 5.215 5.214V6.758a1.001 1.001 0 0 0-1.001-1.001zM23.013 0H11.455a5.215 5.215 0 0 0 5.215 5.215h2.129v2.057A5.215 5.215 0 0 0 24 12.483V1.005A1.005 1.005 0 0 0 23.013 0z"/>
              </svg>
              <span class="text-sm font-medium">Jira</span>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Features Section -->
    <section id="features" class="relative py-24 px-4 sm:px-6 lg:px-8">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-16">
          <h2 class="text-3xl sm:text-4xl lg:text-5xl font-bold text-white mb-4">
            Everything You Need to
            <span class="bg-gradient-to-r from-purple-400 to-blue-400 text-transparent bg-clip-text">Scale</span>
          </h2>
          <p class="text-lg text-white/60 max-w-2xl mx-auto">
            From strategic planning to day-to-day operations, OrbitOS provides the tools and AI intelligence to drive your business forward.
          </p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <NuxtLink
            v-for="feature in features"
            :key="feature.title"
            :to="`/features/${feature.slug}`"
            class="group backdrop-blur-sm bg-white/5 border border-white/10 rounded-2xl p-6 hover:bg-white/10 hover:border-white/20 transition-all duration-300"
          >
            <div
              :class="[
                'inline-flex items-center justify-center w-12 h-12 rounded-xl mb-4',
                feature.color === 'purple' ? 'bg-purple-500/20 text-purple-400' : '',
                feature.color === 'blue' ? 'bg-blue-500/20 text-blue-400' : '',
                feature.color === 'emerald' ? 'bg-emerald-500/20 text-emerald-400' : '',
                feature.color === 'cyan' ? 'bg-cyan-500/20 text-cyan-400' : '',
                feature.color === 'amber' ? 'bg-amber-500/20 text-amber-400' : '',
                feature.color === 'rose' ? 'bg-rose-500/20 text-rose-400' : ''
              ]"
            >
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="feature.icon" />
              </svg>
            </div>
            <h3 class="text-xl font-semibold text-white mb-2 group-hover:text-white transition-colors">
              {{ feature.title }}
            </h3>
            <p class="text-white/60 leading-relaxed">
              {{ feature.description }}
            </p>
            <div class="mt-4 flex items-center gap-1 text-sm font-medium" :class="[
              feature.color === 'purple' ? 'text-purple-400' : '',
              feature.color === 'blue' ? 'text-blue-400' : '',
              feature.color === 'emerald' ? 'text-emerald-400' : '',
              feature.color === 'cyan' ? 'text-cyan-400' : '',
              feature.color === 'amber' ? 'text-amber-400' : '',
              feature.color === 'rose' ? 'text-rose-400' : ''
            ]">
              Learn more
              <svg class="w-4 h-4 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
            </div>
          </NuxtLink>
        </div>
      </div>
    </section>

    <!-- AI Section -->
    <section class="relative py-24 px-4 sm:px-6 lg:px-8">
      <div class="max-w-7xl mx-auto">
        <div class="grid lg:grid-cols-2 gap-12 items-center">
          <div>
            <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-cyan-500/10 border border-cyan-500/20 mb-6">
              <span class="text-cyan-400 text-sm font-medium">AI-Powered</span>
            </div>
            <h2 class="text-3xl sm:text-4xl lg:text-5xl font-bold text-white mb-6">
              AI Agents That
              <span class="bg-gradient-to-r from-cyan-400 to-blue-400 text-transparent bg-clip-text">Understand</span>
              Your Business
            </h2>
            <p class="text-lg text-white/60 mb-8">
              Our AI agents don't just respond - they understand. Connected to your business data, they provide contextual insights, identify risks, and suggest optimizations that actually make sense for your organization.
            </p>
            <ul class="space-y-4">
              <li class="flex items-start gap-3">
                <div class="flex-shrink-0 w-6 h-6 rounded-full bg-emerald-500/20 flex items-center justify-center mt-0.5">
                  <svg class="w-4 h-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <span class="text-white/80">Context-aware responses based on your actual data</span>
              </li>
              <li class="flex items-start gap-3">
                <div class="flex-shrink-0 w-6 h-6 rounded-full bg-emerald-500/20 flex items-center justify-center mt-0.5">
                  <svg class="w-4 h-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <span class="text-white/80">Multi-agent conversations for complex problems</span>
              </li>
              <li class="flex items-start gap-3">
                <div class="flex-shrink-0 w-6 h-6 rounded-full bg-emerald-500/20 flex items-center justify-center mt-0.5">
                  <svg class="w-4 h-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <span class="text-white/80">Action proposals with human approval workflow</span>
              </li>
            </ul>
          </div>
          <div class="backdrop-blur-sm bg-white/5 border border-white/10 rounded-2xl p-6">
            <div class="space-y-4">
              <div class="flex items-start gap-3">
                <div class="w-8 h-8 rounded-lg bg-purple-500/20 flex items-center justify-center flex-shrink-0">
                  <span class="text-purple-400 text-sm font-bold">AI</span>
                </div>
                <div class="flex-1 bg-slate-800/50 rounded-xl rounded-tl-none p-4">
                  <p class="text-white/80 text-sm">Based on your current org structure, I've identified that the "Financial Analysis" function has only one person assigned (Sarah). This creates a single point of failure risk.</p>
                </div>
              </div>
              <div class="flex items-start gap-3 justify-end">
                <div class="flex-1 bg-purple-500/20 rounded-xl rounded-tr-none p-4 max-w-[80%]">
                  <p class="text-white/80 text-sm">What do you recommend we do about it?</p>
                </div>
                <div class="w-8 h-8 rounded-lg bg-blue-500/20 flex items-center justify-center flex-shrink-0">
                  <span class="text-blue-400 text-sm font-bold">You</span>
                </div>
              </div>
              <div class="flex items-start gap-3">
                <div class="w-8 h-8 rounded-lg bg-purple-500/20 flex items-center justify-center flex-shrink-0">
                  <span class="text-purple-400 text-sm font-bold">AI</span>
                </div>
                <div class="flex-1 bg-slate-800/50 rounded-xl rounded-tl-none p-4">
                  <p class="text-white/80 text-sm mb-3">I recommend two options:</p>
                  <div class="space-y-2 text-sm text-white/60">
                    <div class="flex items-start gap-2">
                      <span class="text-purple-400">1.</span>
                      <span>Cross-train Marcus (Operations Analyst) on financial analysis</span>
                    </div>
                    <div class="flex items-start gap-2">
                      <span class="text-purple-400">2.</span>
                      <span>Hire a junior financial analyst to support Sarah</span>
                    </div>
                  </div>
                  <button class="mt-3 px-3 py-1.5 bg-purple-500/20 border border-purple-500/30 rounded-lg text-purple-300 text-xs hover:bg-purple-500/30 transition-colors">
                    Create Action Items
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Testimonials Section -->
    <section id="testimonials" class="relative py-24 px-4 sm:px-6 lg:px-8">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-16">
          <h2 class="text-3xl sm:text-4xl lg:text-5xl font-bold text-white mb-4">
            Trusted by
            <span class="bg-gradient-to-r from-purple-400 to-blue-400 text-transparent bg-clip-text">Operations Leaders</span>
          </h2>
          <p class="text-lg text-white/60 max-w-2xl mx-auto">
            See how forward-thinking companies are using OrbitOS to transform their operations.
          </p>
        </div>

        <div class="grid md:grid-cols-3 gap-8">
          <div
            v-for="testimonial in testimonials"
            :key="testimonial.author"
            class="backdrop-blur-sm bg-white/5 border border-white/10 rounded-2xl p-6 hover:bg-white/[0.07] transition-colors"
          >
            <div class="flex items-center gap-1 mb-4">
              <svg v-for="i in 5" :key="i" class="w-5 h-5 text-amber-400" fill="currentColor" viewBox="0 0 20 20">
                <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
              </svg>
            </div>
            <p class="text-white/80 mb-6 leading-relaxed">
              "{{ testimonial.quote }}"
            </p>
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-full bg-gradient-to-br from-purple-500 to-blue-600 flex items-center justify-center text-white text-sm font-bold">
                {{ testimonial.avatar }}
              </div>
              <div>
                <div class="text-white font-medium">{{ testimonial.author }}</div>
                <div class="text-white/50 text-sm">{{ testimonial.title }}, {{ testimonial.company }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Pricing Section -->
    <section id="pricing" class="relative py-24 px-4 sm:px-6 lg:px-8">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-16">
          <h2 class="text-3xl sm:text-4xl lg:text-5xl font-bold text-white mb-4">
            Simple, Transparent
            <span class="bg-gradient-to-r from-purple-400 to-blue-400 text-transparent bg-clip-text">Pricing</span>
          </h2>
          <p class="text-lg text-white/60 max-w-2xl mx-auto">
            Start with a 14-day free trial. No credit card required.
          </p>
        </div>

        <div class="grid md:grid-cols-3 gap-8 max-w-5xl mx-auto">
          <div
            v-for="tier in pricing"
            :key="tier.name"
            :class="[
              'relative backdrop-blur-sm border rounded-2xl p-8 transition-all duration-300',
              tier.highlighted
                ? 'bg-gradient-to-b from-purple-500/20 to-blue-500/10 border-purple-500/30 scale-105'
                : 'bg-white/5 border-white/10 hover:bg-white/10'
            ]"
          >
            <div v-if="tier.highlighted" class="absolute -top-4 left-1/2 -translate-x-1/2">
              <span class="px-4 py-1 bg-gradient-to-r from-purple-500 to-blue-500 rounded-full text-white text-sm font-medium">
                Most Popular
              </span>
            </div>
            <div class="text-center mb-8">
              <h3 class="text-xl font-semibold text-white mb-2">{{ tier.name }}</h3>
              <div class="flex items-baseline justify-center gap-1 mb-2">
                <span v-if="tier.price !== 'Custom'" class="text-white/40 text-lg">$</span>
                <span class="text-4xl font-bold text-white">{{ tier.price }}</span>
                <span v-if="tier.price !== 'Custom'" class="text-white/40">/mo</span>
              </div>
              <p class="text-white/60 text-sm">{{ tier.description }}</p>
            </div>
            <ul class="space-y-4 mb-8">
              <li v-for="feature in tier.features" :key="feature" class="flex items-center gap-3 text-white/80">
                <svg class="w-5 h-5 text-emerald-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
                {{ feature }}
              </li>
            </ul>
            <NuxtLink
              :to="tier.price === 'Custom' ? '#' : '/login'"
              :class="[
                'block w-full py-3 px-6 rounded-xl font-semibold text-center transition-all',
                tier.highlighted
                  ? 'bg-gradient-to-r from-purple-600 to-blue-600 text-white hover:shadow-lg hover:shadow-purple-500/30'
                  : 'bg-white/10 text-white hover:bg-white/20'
              ]"
              @click="tier.price === 'Custom' ? $event.preventDefault() : null"
            >
              {{ tier.cta }}
            </NuxtLink>
          </div>
        </div>
      </div>
    </section>

    <!-- CTA Section -->
    <section class="relative py-24 px-4 sm:px-6 lg:px-8">
      <div class="max-w-4xl mx-auto">
        <div class="backdrop-blur-xl bg-gradient-to-r from-purple-500/20 via-blue-500/20 to-cyan-500/20 border border-white/10 rounded-3xl p-12 text-center">
          <h2 class="text-3xl sm:text-4xl font-bold text-white mb-4">
            Ready to Transform Your Operations?
          </h2>
          <p class="text-lg text-white/60 mb-8 max-w-2xl mx-auto">
            Join operations leaders who are using OrbitOS to connect strategy to execution and make better decisions with AI.
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
            <a
              href="mailto:sales@orbitos.io"
              class="flex items-center gap-2 px-8 py-4 text-lg font-semibold text-white/80 hover:text-white transition-colors"
            >
              Contact Sales
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
            </a>
          </div>
        </div>
      </div>
    </section>

    <!-- Footer -->
    <footer class="relative py-12 px-4 sm:px-6 lg:px-8 border-t border-white/5">
      <div class="max-w-7xl mx-auto">
        <div class="grid grid-cols-2 md:grid-cols-4 gap-8 mb-12">
          <div>
            <h4 class="text-white font-semibold mb-4">Features</h4>
            <ul class="space-y-2">
              <li><NuxtLink to="/features/business-model-canvas" class="text-white/60 hover:text-white transition-colors text-sm">Business Model Canvas</NuxtLink></li>
              <li><NuxtLink to="/features/organizational-intelligence" class="text-white/60 hover:text-white transition-colors text-sm">Organizational Intelligence</NuxtLink></li>
              <li><NuxtLink to="/features/process-automation" class="text-white/60 hover:text-white transition-colors text-sm">Process Automation</NuxtLink></li>
              <li><NuxtLink to="/features/ai-agents" class="text-white/60 hover:text-white transition-colors text-sm">AI Agents</NuxtLink></li>
            </ul>
          </div>
          <div>
            <h4 class="text-white font-semibold mb-4">Company</h4>
            <ul class="space-y-2">
              <li><a href="mailto:hello@orbitos.io" class="text-white/60 hover:text-white transition-colors text-sm">Contact</a></li>
            </ul>
          </div>
          <div>
            <h4 class="text-white font-semibold mb-4">Resources</h4>
            <ul class="space-y-2">
              <li><a href="mailto:support@orbitos.io" class="text-white/60 hover:text-white transition-colors text-sm">Support</a></li>
            </ul>
          </div>
          <div>
            <h4 class="text-white font-semibold mb-4">Legal</h4>
            <ul class="space-y-2">
              <li><NuxtLink to="/privacy" class="text-white/60 hover:text-white transition-colors text-sm">Privacy Policy</NuxtLink></li>
              <li><NuxtLink to="/terms" class="text-white/60 hover:text-white transition-colors text-sm">Terms of Service</NuxtLink></li>
            </ul>
          </div>
        </div>
        <div class="flex flex-col sm:flex-row items-center justify-between pt-8 border-t border-white/5">
          <div class="flex items-center gap-3 mb-4 sm:mb-0">
            <div class="flex items-center justify-center w-8 h-8 rounded-lg bg-gradient-to-br from-purple-500 to-blue-600">
              <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3c.132 0 .263 0 .393 0a7.5 7.5 0 0 0 7.92 12.446a9 9 0 1 1 -8.313-12.454z" />
              </svg>
            </div>
            <span class="text-white/60 text-sm">&copy; 2026 OrbitOS. All rights reserved.</span>
          </div>
          <div class="flex items-center gap-4">
            <a href="https://twitter.com/orbitos" target="_blank" rel="noopener noreferrer" class="text-white/40 hover:text-white transition-colors">
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24"><path d="M24 4.557c-.883.392-1.832.656-2.828.775 1.017-.609 1.798-1.574 2.165-2.724-.951.564-2.005.974-3.127 1.195-.897-.957-2.178-1.555-3.594-1.555-3.179 0-5.515 2.966-4.797 6.045-4.091-.205-7.719-2.165-10.148-5.144-1.29 2.213-.669 5.108 1.523 6.574-.806-.026-1.566-.247-2.229-.616-.054 2.281 1.581 4.415 3.949 4.89-.693.188-1.452.232-2.224.084.626 1.956 2.444 3.379 4.6 3.419-2.07 1.623-4.678 2.348-7.29 2.04 2.179 1.397 4.768 2.212 7.548 2.212 9.142 0 14.307-7.721 13.995-14.646.962-.695 1.797-1.562 2.457-2.549z"/></svg>
            </a>
            <a href="https://linkedin.com/company/orbitos" target="_blank" rel="noopener noreferrer" class="text-white/40 hover:text-white transition-colors">
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24"><path d="M19 0h-14c-2.761 0-5 2.239-5 5v14c0 2.761 2.239 5 5 5h14c2.762 0 5-2.239 5-5v-14c0-2.761-2.238-5-5-5zm-11 19h-3v-11h3v11zm-1.5-12.268c-.966 0-1.75-.79-1.75-1.764s.784-1.764 1.75-1.764 1.75.79 1.75 1.764-.783 1.764-1.75 1.764zm13.5 12.268h-3v-5.604c0-3.368-4-3.113-4 0v5.604h-3v-11h3v1.765c1.396-2.586 7-2.777 7 2.476v6.759z"/></svg>
            </a>
          </div>
        </div>
      </div>
    </footer>
  </div>
</template>

<style scoped>
@keyframes blob {
  0% { transform: translate(0px, 0px) scale(1); }
  33% { transform: translate(30px, -50px) scale(1.1); }
  66% { transform: translate(-20px, 20px) scale(0.9); }
  100% { transform: translate(0px, 0px) scale(1); }
}

.animate-blob {
  animation: blob 7s infinite;
}

.animation-delay-2000 {
  animation-delay: 2s;
}

.animation-delay-4000 {
  animation-delay: 4s;
}
</style>
