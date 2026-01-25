<script setup lang="ts">
// No special layout needed - uses default
</script>

<template>
  <div class="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 relative">
    <!-- Navigation -->
    <nav class="fixed top-0 left-0 right-0 z-50 backdrop-blur-xl bg-slate-950/70 border-b border-white/5">
      <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between h-16">
          <NuxtLink to="/" class="flex items-center gap-3">
            <div class="flex items-center justify-center w-10 h-10 rounded-xl bg-gradient-to-br from-purple-500 to-blue-600 shadow-lg shadow-purple-500/20">
              <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3c.132 0 .263 0 .393 0a7.5 7.5 0 0 0 7.92 12.446a9 9 0 1 1 -8.313-12.454z" />
              </svg>
            </div>
            <span class="text-xl font-bold text-white">OrbitOS</span>
          </NuxtLink>
          <NuxtLink to="/" class="text-white/60 hover:text-white text-sm font-medium transition-colors">
            Back to Home
          </NuxtLink>
        </div>
      </div>
    </nav>

    <!-- Content -->
    <div class="pt-32 pb-20 px-4 sm:px-6 lg:px-8">
      <div class="max-w-4xl mx-auto">
        <h1 class="text-4xl font-bold text-white mb-4">Privacy Policy</h1>
        <p class="text-white/60 mb-8">Last updated: January 2026</p>

        <div class="prose prose-invert prose-lg max-w-none">
          <div class="space-y-8 text-white/80">
            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">1. Introduction</h2>
              <p>
                OrbitOS ("we", "our", or "us") is committed to protecting your privacy. This Privacy Policy explains how we collect, use, disclose, and safeguard your information when you use our AI-native business operating system platform.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">2. Information We Collect</h2>
              <h3 class="text-xl font-medium text-white/90 mb-2">2.1 Information You Provide</h3>
              <ul class="list-disc list-inside space-y-2 ml-4">
                <li>Account information (name, email, password)</li>
                <li>Organization details and business data you input</li>
                <li>Content from AI conversations and interactions</li>
                <li>Payment and billing information</li>
              </ul>

              <h3 class="text-xl font-medium text-white/90 mb-2 mt-4">2.2 Automatically Collected Information</h3>
              <ul class="list-disc list-inside space-y-2 ml-4">
                <li>Device and browser information</li>
                <li>IP address and location data</li>
                <li>Usage patterns and analytics</li>
                <li>Cookies and similar technologies</li>
              </ul>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">3. How We Use Your Information</h2>
              <ul class="list-disc list-inside space-y-2 ml-4">
                <li>To provide and maintain our services</li>
                <li>To personalize your experience and AI interactions</li>
                <li>To process transactions and send related information</li>
                <li>To send administrative information and updates</li>
                <li>To respond to your comments and questions</li>
                <li>To improve our services and develop new features</li>
                <li>To detect and prevent fraud and abuse</li>
              </ul>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">4. AI and Your Data</h2>
              <p>
                Our AI agents process your business data to provide contextual insights and recommendations. We:
              </p>
              <ul class="list-disc list-inside space-y-2 ml-4 mt-2">
                <li>Do not use your data to train our AI models without explicit consent</li>
                <li>Keep your business data strictly within your organization's context</li>
                <li>Provide full audit trails of AI interactions</li>
                <li>Allow you to delete AI conversation history at any time</li>
              </ul>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">5. Data Sharing and Disclosure</h2>
              <p>We may share your information with:</p>
              <ul class="list-disc list-inside space-y-2 ml-4 mt-2">
                <li>Service providers who assist in our operations</li>
                <li>Professional advisors (lawyers, accountants, auditors)</li>
                <li>Authorities when required by law</li>
                <li>Business partners with your consent</li>
              </ul>
              <p class="mt-4">We do not sell your personal information to third parties.</p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">6. Data Security</h2>
              <p>
                We implement appropriate technical and organizational measures to protect your data, including:
              </p>
              <ul class="list-disc list-inside space-y-2 ml-4 mt-2">
                <li>Encryption in transit and at rest</li>
                <li>SOC 2 Type II compliance</li>
                <li>Regular security audits and penetration testing</li>
                <li>Role-based access controls</li>
                <li>Multi-factor authentication options</li>
              </ul>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">7. Your Rights</h2>
              <p>Depending on your location, you may have the right to:</p>
              <ul class="list-disc list-inside space-y-2 ml-4 mt-2">
                <li>Access your personal data</li>
                <li>Correct inaccurate data</li>
                <li>Delete your data</li>
                <li>Export your data in a portable format</li>
                <li>Opt out of certain data processing</li>
                <li>Withdraw consent where applicable</li>
              </ul>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">8. Data Retention</h2>
              <p>
                We retain your data for as long as your account is active or as needed to provide services. After account deletion, we may retain certain information for legal compliance, dispute resolution, or legitimate business purposes.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">9. International Transfers</h2>
              <p>
                Your data may be transferred to and processed in countries other than your own. We ensure appropriate safeguards are in place for such transfers in compliance with applicable data protection laws.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">10. Contact Us</h2>
              <p>
                If you have questions about this Privacy Policy or our data practices, please contact us at:
              </p>
              <p class="mt-2">
                <a href="mailto:privacy@orbitos.io" class="text-purple-400 hover:text-purple-300 transition-colors">
                  privacy@orbitos.io
                </a>
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">11. Changes to This Policy</h2>
              <p>
                We may update this Privacy Policy from time to time. We will notify you of any material changes by posting the new policy on this page and updating the "Last updated" date.
              </p>
            </section>
          </div>
        </div>
      </div>
    </div>

    <!-- Footer -->
    <footer class="py-8 px-4 border-t border-white/5">
      <div class="max-w-4xl mx-auto flex items-center justify-between">
        <span class="text-white/40 text-sm">&copy; 2026 OrbitOS. All rights reserved.</span>
        <div class="flex gap-6">
          <NuxtLink to="/privacy" class="text-white/60 hover:text-white text-sm transition-colors">Privacy</NuxtLink>
          <NuxtLink to="/terms" class="text-white/60 hover:text-white text-sm transition-colors">Terms</NuxtLink>
        </div>
      </div>
    </footer>
  </div>
</template>
