<script setup lang="ts">
import { useAuth } from '~/composables/useAuth'

const { isAuthenticated, isLoading, initializeAuth } = useAuth()

onMounted(() => {
  initializeAuth()
})

// Scroll to section
const scrollToSection = (id: string) => {
  const element = document.getElementById(id)
  if (element) {
    element.scrollIntoView({ behavior: 'smooth' })
  }
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

// Testimonials/logos data
const trustedBy = [
  'Fortune 500 Companies',
  'Fast-growing Startups',
  'Management Consultancies',
  'Private Equity Firms'
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

          <!-- Nav Links -->
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

            <button @click="scrollToSection('pricing')" class="text-white/60 hover:text-white transition-colors text-sm font-medium">Pricing</button>
          </div>

          <!-- Auth Buttons -->
          <div class="flex items-center gap-3">
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
          Run Your Business
          <br />
          <span class="bg-gradient-to-r from-purple-400 via-blue-400 to-cyan-400 text-transparent bg-clip-text">
            Like Never Before
          </span>
        </h1>

        <!-- Subheadline -->
        <p class="text-lg sm:text-xl text-white/60 max-w-3xl mx-auto mb-10">
          OrbitOS connects your strategy, operations, and people in one intelligent platform.
          AI agents that understand your business context help you make better decisions, faster.
        </p>

        <!-- CTA Buttons -->
        <div class="flex flex-col sm:flex-row items-center justify-center gap-4 mb-16">
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
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            Watch Demo
          </button>
        </div>

        <!-- Social Proof -->
        <div class="flex flex-col items-center">
          <p class="text-white/40 text-sm mb-6">Trusted by forward-thinking organizations</p>
          <div class="flex flex-wrap justify-center gap-8">
            <div v-for="company in trustedBy" :key="company" class="text-white/30 text-sm font-medium">
              {{ company }}
            </div>
          </div>
        </div>

        <!-- Hero Image/Screenshot -->
        <div class="mt-20 relative">
          <div class="absolute inset-0 bg-gradient-to-t from-slate-950 via-transparent to-transparent z-10"></div>
          <div class="backdrop-blur-sm bg-white/5 border border-white/10 rounded-3xl p-4 shadow-2xl shadow-purple-500/10">
            <div class="bg-slate-900 rounded-2xl overflow-hidden aspect-video flex items-center justify-center">
              <div class="text-center p-8">
                <div class="inline-flex items-center justify-center w-20 h-20 rounded-2xl bg-gradient-to-br from-purple-500/20 to-blue-600/20 border border-white/10 mb-6">
                  <svg class="w-10 h-10 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
                  </svg>
                </div>
                <p class="text-white/40 text-lg">Interactive Dashboard Preview</p>
                <p class="text-white/20 text-sm mt-2">Business Model Canvas + AI Insights</p>
              </div>
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
          <div
            v-for="feature in features"
            :key="feature.title"
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
          </div>
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
              to="/login"
              :class="[
                'block w-full py-3 px-6 rounded-xl font-semibold text-center transition-all',
                tier.highlighted
                  ? 'bg-gradient-to-r from-purple-600 to-blue-600 text-white hover:shadow-lg hover:shadow-purple-500/30'
                  : 'bg-white/10 text-white hover:bg-white/20'
              ]"
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
            Join hundreds of forward-thinking organizations using OrbitOS to streamline their business operations.
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
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">About</a></li>
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">Blog</a></li>
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">Careers</a></li>
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">Contact</a></li>
            </ul>
          </div>
          <div>
            <h4 class="text-white font-semibold mb-4">Resources</h4>
            <ul class="space-y-2">
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">Help Center</a></li>
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">API Reference</a></li>
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">Status</a></li>
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">Community</a></li>
            </ul>
          </div>
          <div>
            <h4 class="text-white font-semibold mb-4">Legal</h4>
            <ul class="space-y-2">
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">Privacy Policy</a></li>
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">Terms of Service</a></li>
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">Security</a></li>
              <li><a href="#" class="text-white/60 hover:text-white transition-colors text-sm">SOC 2</a></li>
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
            <a href="#" class="text-white/40 hover:text-white transition-colors">
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24"><path d="M24 4.557c-.883.392-1.832.656-2.828.775 1.017-.609 1.798-1.574 2.165-2.724-.951.564-2.005.974-3.127 1.195-.897-.957-2.178-1.555-3.594-1.555-3.179 0-5.515 2.966-4.797 6.045-4.091-.205-7.719-2.165-10.148-5.144-1.29 2.213-.669 5.108 1.523 6.574-.806-.026-1.566-.247-2.229-.616-.054 2.281 1.581 4.415 3.949 4.89-.693.188-1.452.232-2.224.084.626 1.956 2.444 3.379 4.6 3.419-2.07 1.623-4.678 2.348-7.29 2.04 2.179 1.397 4.768 2.212 7.548 2.212 9.142 0 14.307-7.721 13.995-14.646.962-.695 1.797-1.562 2.457-2.549z"/></svg>
            </a>
            <a href="#" class="text-white/40 hover:text-white transition-colors">
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24"><path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/></svg>
            </a>
            <a href="#" class="text-white/40 hover:text-white transition-colors">
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
