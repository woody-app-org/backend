package com.woody.backend.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.core.userdetails.User;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.provisioning.InMemoryUserDetailsManager;
import org.springframework.security.web.SecurityFilterChain;

@Configuration
@EnableWebSecurity
public class WebSecurityConfig {
    @Bean
    public SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
        http
        .csrf(csrf-> csrf.disable()) // desativa csrf global
        .headers(
            headers -> headers
                        .frameOptions(frame -> frame.sameOrigin()) //permite iframes do mesmo domínio
        )
        .authorizeHttpRequests(
            auth -> auth
                        .requestMatchers("/", "/h2-console/**").permitAll()
                        .anyRequest().authenticated() // exige login para todos os endpoints menos ("/")   
        )
        .httpBasic(Customizer.withDefaults()); //autenticação básica, não precisa de formulário de login

        return http.build();
    }

    @Bean
    public UserDetailsService userDetailsService(){ // Cria um usuario em memória 
        UserDetails user = User
                            .withUsername("user")
                            .password("{noop}1234")
                            .roles("USER")
                            .build();

        return new InMemoryUserDetailsManager(user);
    }
}
