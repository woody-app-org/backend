package com.woody.backend.repositories;

import static org.junit.jupiter.api.Assertions.assertEquals;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;

import com.woody.backend.infraestructure.entity.Hello;
import com.woody.backend.infraestructure.repository.interfaces.HelloRepository;

import jakarta.transaction.Transactional;

@SpringBootTest
@ActiveProfiles("test")
@Transactional
public class HelloRepositoryTest {
    
    @Autowired
    private HelloRepository helloRepository;

    @Test
    public void shouldReturnHelloTest() {
        var hello = this.helloRepository.get();

        assertEquals("Hello Test!", hello.get(0).getHello());
    }
}
