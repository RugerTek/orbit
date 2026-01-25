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
        <h1 class="text-4xl font-bold text-white mb-4">Terms of Service</h1>
        <p class="text-white/60 mb-8">Last updated: January 2026</p>

        <div class="prose prose-invert prose-lg max-w-none">
          <div class="space-y-8 text-white/80">
            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">1. Acceptance of Terms</h2>
              <p>
                By accessing or using OrbitOS ("the Service"), you agree to be bound by these Terms of Service ("Terms"). If you disagree with any part of these terms, you may not access the Service.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">2. Description of Service</h2>
              <p>
                OrbitOS is an AI-native business operating system that provides:
              </p>
              <ul class="list-disc list-inside space-y-2 ml-4 mt-2">
                <li>Business model canvas management and visualization</li>
                <li>Organizational intelligence and structure mapping</li>
                <li>Process design and automation tools</li>
                <li>Multi-agent AI chat capabilities</li>
                <li>Analytics and insights dashboards</li>
              </ul>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">3. User Accounts</h2>
              <h3 class="text-xl font-medium text-white/90 mb-2">3.1 Registration</h3>
              <p>
                You must register for an account to use the Service. You agree to provide accurate, current, and complete information during registration and to update such information to keep it accurate, current, and complete.
              </p>

              <h3 class="text-xl font-medium text-white/90 mb-2 mt-4">3.2 Account Security</h3>
              <p>
                You are responsible for safeguarding your password and for all activities that occur under your account. You agree to notify us immediately of any unauthorized use of your account.
              </p>

              <h3 class="text-xl font-medium text-white/90 mb-2 mt-4">3.3 Organizations</h3>
              <p>
                If you create or administer an organization within OrbitOS, you are responsible for all users and data within that organization.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">4. Acceptable Use</h2>
              <p>You agree not to:</p>
              <ul class="list-disc list-inside space-y-2 ml-4 mt-2">
                <li>Use the Service for any illegal purpose</li>
                <li>Violate any applicable laws or regulations</li>
                <li>Infringe on the rights of others</li>
                <li>Attempt to gain unauthorized access to the Service</li>
                <li>Interfere with or disrupt the Service</li>
                <li>Upload malicious code or content</li>
                <li>Use the Service to send spam or unsolicited communications</li>
                <li>Reverse engineer or attempt to extract source code</li>
              </ul>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">5. Intellectual Property</h2>
              <h3 class="text-xl font-medium text-white/90 mb-2">5.1 Our Property</h3>
              <p>
                The Service and its original content, features, and functionality are owned by OrbitOS and are protected by international copyright, trademark, patent, trade secret, and other intellectual property laws.
              </p>

              <h3 class="text-xl font-medium text-white/90 mb-2 mt-4">5.2 Your Content</h3>
              <p>
                You retain ownership of all content you submit to the Service. By submitting content, you grant us a license to use, store, and process that content to provide the Service.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">6. AI Features</h2>
              <p>
                Our AI features are designed to assist with business operations. You acknowledge that:
              </p>
              <ul class="list-disc list-inside space-y-2 ml-4 mt-2">
                <li>AI-generated content may not always be accurate</li>
                <li>You are responsible for reviewing and verifying AI recommendations</li>
                <li>AI agents access your organization data to provide contextual responses</li>
                <li>You maintain control over AI-proposed actions through approval workflows</li>
              </ul>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">7. Subscription and Payment</h2>
              <h3 class="text-xl font-medium text-white/90 mb-2">7.1 Fees</h3>
              <p>
                Certain features of the Service require payment. Fees are described on our pricing page and are subject to change with notice.
              </p>

              <h3 class="text-xl font-medium text-white/90 mb-2 mt-4">7.2 Billing</h3>
              <p>
                Subscriptions are billed in advance on a monthly or annual basis. You authorize us to charge your payment method for all fees due.
              </p>

              <h3 class="text-xl font-medium text-white/90 mb-2 mt-4">7.3 Cancellation</h3>
              <p>
                You may cancel your subscription at any time. Cancellation takes effect at the end of the current billing period.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">8. Data and Privacy</h2>
              <p>
                Your use of the Service is also governed by our <NuxtLink to="/privacy" class="text-purple-400 hover:text-purple-300 transition-colors">Privacy Policy</NuxtLink>, which is incorporated into these Terms by reference.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">9. Disclaimer of Warranties</h2>
              <p>
                THE SERVICE IS PROVIDED "AS IS" AND "AS AVAILABLE" WITHOUT WARRANTIES OF ANY KIND, EXPRESS OR IMPLIED. WE DO NOT WARRANT THAT THE SERVICE WILL BE UNINTERRUPTED, SECURE, OR ERROR-FREE.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">10. Limitation of Liability</h2>
              <p>
                TO THE MAXIMUM EXTENT PERMITTED BY LAW, ORBITOS SHALL NOT BE LIABLE FOR ANY INDIRECT, INCIDENTAL, SPECIAL, CONSEQUENTIAL, OR PUNITIVE DAMAGES, OR ANY LOSS OF PROFITS OR REVENUES.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">11. Indemnification</h2>
              <p>
                You agree to indemnify and hold harmless OrbitOS and its officers, directors, employees, and agents from any claims, damages, or expenses arising from your use of the Service or violation of these Terms.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">12. Termination</h2>
              <p>
                We may terminate or suspend your account and access to the Service immediately, without prior notice, for conduct that we believe violates these Terms or is harmful to other users, us, or third parties.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">13. Changes to Terms</h2>
              <p>
                We reserve the right to modify these Terms at any time. We will provide notice of material changes by posting the updated Terms on this page and updating the "Last updated" date.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">14. Governing Law</h2>
              <p>
                These Terms shall be governed by and construed in accordance with the laws of the jurisdiction in which OrbitOS is incorporated, without regard to its conflict of law provisions.
              </p>
            </section>

            <section>
              <h2 class="text-2xl font-semibold text-white mb-4">15. Contact Us</h2>
              <p>
                If you have any questions about these Terms, please contact us at:
              </p>
              <p class="mt-2">
                <a href="mailto:legal@orbitos.io" class="text-purple-400 hover:text-purple-300 transition-colors">
                  legal@orbitos.io
                </a>
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
